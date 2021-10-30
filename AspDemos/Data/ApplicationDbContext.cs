using System.Security.Claims;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspDemos.Models;
using AspDemos.Areas.Admin.Models.Employees;
using AspDemos.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SoftDeleteServices.Concrete;
using AspDemos.Models.Shared;
using AspDemos.Models.Identity;
using AspDemos.Infrastructure.SoftDelete;

namespace AspDemos.Data {
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) :
            base(options) {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Audit> AuditLogs { get; set; }
        public DbSet<Employee> Employee { get; set; }


        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            foreach (var entityType in builder.Model.GetEntityTypes()) {
                var baseType = entityType.ClrType.BaseType;
                bool inheritedFromMS = baseType?.FullName?.ToString().ToLower().Contains("microsoft") ?? false;
                bool inheritedFromBaseEntity = baseType?.FullName?.ToString().ToLower().Contains("baseentity") ?? false;

                // Universal SQL types for performance
                if (entityType.Name.ToLower().Contains("microsoft")) continue;
                if (entityType.Name.ToLower().Contains("audit")) continue;
                System.Diagnostics.Debug.WriteLine($"||| {entityType.Name}");
                foreach (var entityProperty in entityType.GetProperties()) {
                    if (entityProperty.IsKey()) continue;
                    if (entityProperty.IsForeignKey()) continue;

                    bool inheritedProperty = baseType?.GetProperty(entityProperty.Name) != null;
                    if (inheritedProperty && inheritedFromMS) continue;

                    var annotations = entityProperty.GetAnnotations();
                    bool hasMaxLength = entityProperty.FindAnnotation("MaxLength") != null;
                    var columnType = entityProperty.FindAnnotation("Relational:ColumnType");
                    bool hasVarchar = columnType?.Value?.ToString()?.ToLower().Contains("varchar") ?? false;
                    bool hasSpecifiedVarchar = columnType?.Value?.ToString()?.ToLower().Contains("varchar(") ?? false;
                    bool hasSpecifiedDecimal = columnType?.Value?.ToString()?.ToLower().Contains("decimal(") ?? false;

                    if (entityProperty.ClrType == typeof(decimal) && !hasSpecifiedDecimal) {
                        entityProperty.SetPrecision(9);
                        entityProperty.SetScale(2);
                    }

                    if (entityProperty.ClrType == typeof(string) && !hasMaxLength && !hasSpecifiedVarchar) {
                        entityProperty.SetMaxLength(255);
                    }

                    if (entityProperty.ClrType == typeof(string)
                        && entityProperty.Name.EndsWith("Url")) {
                        entityProperty.SetIsUnicode(false);
                    }
                }


                // Universal cascade soft delete for BaseEntity
                if (typeof(ICascadeSoftDelete).IsAssignableFrom(entityType.ClrType) && !builder.Model.IsOwned(entityType.ClrType)) {
                    entityType.SetCascadeQueryFilter(CascadeQueryFilterTypes.CascadeSoftDelete);
                }
                
                if (typeof(ISingleSoftDelete).IsAssignableFrom(entityType.ClrType))
                    entityType.SetCascadeQueryFilter(CascadeQueryFilterTypes.CascadeAndSingle);
            

            }


        }


        /*
        public override int SaveChanges() {
            //UpdateSoftDeleteStatuses();
            //UpdateAuditLogs();
            var result = base.SaveChanges();
            return result;
        }
        */

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            var result = await base.SaveChangesAsync(cancellationToken);
            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken)) {
            var userId = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            UpdateEntryAudit(userId);
           // await UpdateSoftDeleteStatuses(userId);
            UpdateAuditLogs(userId);
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            return result;
        }

        private async Task UpdateSoftDeleteStatuses(string userId) {
            var allEntries = ChangeTracker.Entries();

            var entries = ChangeTracker
                .Entries()
                .Where(e => typeof(ICascadeSoftDelete).IsAssignableFrom(e.Entity.GetType()) && (
                    e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted));

            foreach (var entry in entries) {
                switch (entry.State) {
                    case EntityState.Added:
                        //entry.CurrentValues["SoftDeleteLevel"] = (byte)0;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        var service = this.GetService<CascadeSoftDelServiceAsync<ICascadeSoftDelete>>();
                        var res = await service.SetCascadeSoftDeleteAsync((ICascadeSoftDelete)entry.Entity);

                        /*
                        // Setting to Unchanged rather than Modified will only update the changed fields in the audit log
                        entry.State = EntityState.Unchanged;

                        // The user who updated the soft delete status may not have changed - update it anyway to preserve audit log
                        entry.CurrentValues["UpdatedBy"] = userId;
                        entry.CurrentValues["SoftDeleteLevel"] = (byte)1;

                        // Detach owned entities so they are not deleted
                        var ownedEntities = entry.References.Where(r => r.TargetEntry != null && r.TargetEntry.Metadata.IsOwned());
                        foreach (var ownedEntity in ownedEntities) {
                            if (typeof(ICascadeSoftDelete).IsAssignableFrom(ownedEntity.TargetEntry.GetType())) {
                                ownedEntity.TargetEntry.CurrentValues["UpdatedBy"] = entry.CurrentValues["UpdatedBy"];
                                ownedEntity.TargetEntry.CurrentValues["SoftDeleteLevel"] = (byte)1;
                            } else {
                                ownedEntity.TargetEntry.State = EntityState.Detached;
                            }
                        }
                        */
                        break;
                }
            }
        }

        private void UpdateEntryAudit(string userId) {
            // Update tags on record
            var entries = ChangeTracker.Entries()
                            .Where(e => e.Entity is BaseEntity && (
                                e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries) {
                BaseEntity entity = ((BaseEntity)entry.Entity);

                if (entity != null) {
                    DateTime now = DateTime.UtcNow;

                    if (entry.State == EntityState.Added) {
                        entity.CreatedDate = now;
                        entity.CreatedBy = userId;
                    } else {
                        base.Entry(entity).Property(x => x.CreatedBy).IsModified = false;
                        base.Entry(entity).Property(x => x.CreatedDate).IsModified = false;
                    }
                    entity.UpdatedDate = now;
                    entity.UpdatedBy = userId;
                }
            }
        }

        private void UpdateAuditLogs(string userId) {
            

            // Update Audit Log Table
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var allEntries = ChangeTracker.Entries();
            foreach (var entry in allEntries) {
                if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;
                var auditEntry = new AuditEntry(entry) {
                    TableName = entry.Entity.GetType().Name,
                    UserId = userId
                };
                auditEntries.Add(auditEntry);
                foreach (var property in entry.Properties) {
                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey()) {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }
                    switch (entry.State) {
                        case EntityState.Added:
                            auditEntry.AuditType = Enums.AuditType.Create;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;
                        case EntityState.Deleted:
                            auditEntry.AuditType = Enums.AuditType.Delete;
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;
                        case EntityState.Modified:
                            if (property.IsModified) {
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.AuditType = Enums.AuditType.Update;
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }
            }
            foreach (var auditEntry in auditEntries) {
                AuditLogs.Add(auditEntry.ToAudit());
            }
        }

    }
}