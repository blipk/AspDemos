using SoftDeleteServices.Configuration;
using AspDemos.Data;
using Microsoft.EntityFrameworkCore;

namespace AspDemos.Infrastructure.SoftDelete {
    public class ConfigCascadeDelete : CascadeSoftDeleteConfiguration<ICascadeSoftDelete> {

        public ConfigCascadeDelete(ApplicationDbContext context) : base(context) {
            GetSoftDeleteValue = entity => entity.SoftDeleteLevel;
            SetSoftDeleteValue = (entity, value) => { entity.SoftDeleteLevel = value; };
            /*
            GetSoftDeleteValue = entity => (byte)context.Entry(entity).Property("SoftDeleteLevel").CurrentValue;
            QuerySoftDeleteValue = entity => EF.Property<byte>(entity, "SoftDeleteLevel");
            SetSoftDeleteValue = (entity, value) => context.Entry(entity).Property("SoftDeleteLevel").CurrentValue = value;
            */
        }

    }
}