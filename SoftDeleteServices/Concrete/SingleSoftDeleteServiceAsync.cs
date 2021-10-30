﻿// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Concrete.Internal;
using SoftDeleteServices.Configuration;
using StatusGeneric;

namespace SoftDeleteServices.Concrete
{
    /// <summary>
    /// This service handles single soft delete, i.e. it only soft deletes a single entity by setting a boolean flag in that entity
    /// </summary>
    /// <typeparam name="TInterface">You provide the interface you applied to your entity classes to require a boolean flag</typeparam>
    public class SingleSoftDeleteServiceAsync<TInterface>
        where TInterface : class
    {
        private readonly DbContext _context;
        private readonly SingleSoftDeleteConfiguration<TInterface> _config;

        /// <summary>
        /// Ctor for SoftDeleteService
        /// </summary>
        /// <param name="config"></param>
        public SingleSoftDeleteServiceAsync(SingleSoftDeleteConfiguration<TInterface> config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _context = config.Context ?? throw new ArgumentNullException(nameof(config), "You must provide the DbContext");

            if (_config.GetSoftDeleteValue == null)
                throw new InvalidOperationException($"You must set the {nameof(_config.GetSoftDeleteValue)} with a query to get the soft delete bool");
            if (_config.SetSoftDeleteValue == null)
                throw new InvalidOperationException($"You must set the {nameof(_config.SetSoftDeleteValue)} with a function to set the value of the soft delete bool");
        }

        /// <summary>
        /// This finds the entity using its primary key(s) and then set the single soft delete flag so it is hidden
        /// </summary>
        /// <param name="keyValues">primary key values</param>
        /// <returns>Returns status. If not errors then Result return 1 to say it worked. Zero if error of Not Found and notFoundAllowed is true</returns>
        public async Task<IStatusGeneric<int>> SetSoftDeleteViaKeysAsync<TEntity>(params object[] keyValues)
            where TEntity : class, TInterface
        {
            return await CheckExecuteSoftDeleteAsync<TEntity>(SetSoftDeleteAsync, keyValues);
        }

        /// <summary>
        /// This finds the entity using its primary key(s) and then it resets the single soft delete flag so it is now visible
        /// </summary>
        /// <param name="keyValues">primary key values</param>
        /// <returns>Returns status. If not errors then Result return 1 to say it worked. Zero if error of Not Found and notFoundAllowed is true</returns>
        public async Task<IStatusGeneric<int>> ResetSoftDeleteViaKeysAsync<TEntity>(params object[] keyValues)
            where TEntity : class, TInterface
        {
            return await CheckExecuteSoftDeleteAsync<TEntity>(ResetSoftDeleteAsync, keyValues);
        }

        /// <summary>
        /// This finds the entity using its primary key(s) and then hard deletes it as long as the soft delete flag is set
        /// </summary>
        /// <param name="keyValues">primary key values</param>
        /// <returns>Returns status. If not errors then Result return 1 to say it worked. Zero if error of Not Found and notFoundAllowed is true</returns>
        public async Task<IStatusGeneric<int>> HardDeleteViaKeysAsync<TEntity>(params object[] keyValues)
            where TEntity : class, TInterface
        {
            return await CheckExecuteSoftDeleteAsync<TEntity>(HardDeleteSoftDeletedEntryAsync, keyValues);
        }

        /// <summary>
        /// This will soft delete the single entity. This may delete other dependent 
        /// </summary>
        /// <param name="softDeleteThisEntity">Mustn't be null</param>
        /// <param name="callSaveChanges">Defaults to calling SaveChanges. If set to false, then you must call SaveChanges</param>
        /// <returns>Returns status. If not errors then Result return 1 to say it worked. Zero if error</returns>
        public async Task<IStatusGeneric<int>> SetSoftDeleteAsync(TInterface softDeleteThisEntity, bool callSaveChanges = true)
        {
            if (softDeleteThisEntity == null) throw new ArgumentNullException(nameof(softDeleteThisEntity));
            _context.ThrowExceptionIfPrincipalOneToOne(softDeleteThisEntity);

            var status = new StatusGenericHandler<int>();
            if (_config.GetSoftDeleteValue.Compile().Invoke(softDeleteThisEntity))
                return status.AddError($"This entry is already {_config.TextSoftDeletedPastTense}.");

            _config.SetSoftDeleteValue(softDeleteThisEntity, true);
            if (callSaveChanges)
                await _context.SaveChangesAsync();

            status.Message = $"Successfully {_config.TextSoftDeletedPastTense} this entry";
            status.SetResult(1);        //one changed
            return status;
        }



        /// <summary>
        /// This resets the single soft delete flag so that entity is now visible
        /// </summary>
        /// <param name="resetSoftDeleteThisEntity">Mustn't be null</param>
        /// <param name="callSaveChanges">Defaults to calling SaveChanges. If set to false, then you must call SaveChanges yourself</param>
        /// <returns>Returns status. If not errors then Result return 1 to say it worked. Zero if errors</returns>
        public async Task<IStatusGeneric<int>> ResetSoftDeleteAsync(TInterface resetSoftDeleteThisEntity, bool callSaveChanges = true)
        {
            if (resetSoftDeleteThisEntity == null) throw new ArgumentNullException(nameof(resetSoftDeleteThisEntity));

            var status = new StatusGenericHandler<int>();
            if (!_config.GetSoftDeleteValue.Compile().Invoke(resetSoftDeleteThisEntity))
                return status.AddError($"This entry isn't {_config.TextSoftDeletedPastTense}.");

            _config.SetSoftDeleteValue(resetSoftDeleteThisEntity, false);
            if (callSaveChanges)
                await _context.SaveChangesAsync();

            status.Message = $"Successfully {_config.TextResetSoftDelete} on this entry";
            status.SetResult(1);   //One changed
            return status;
        }

        /// <summary>
        /// This hard deletes (i.e. calls EF Core's Remove method) for this entity ONLY if it first been soft deleted.
        /// This will delete the entity and possibly delete other dependent entities.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="hardDeleteThisEntity">The entity to delete</param>
        /// <param name="callSaveChanges">Defaults to calling SaveChanges. If set to false, then you must call SaveChanges yourself</param>
        /// <returns>The number of entities that were deleted. This will include any dependent entities that that had a cascade delete behaviour</returns>
        public async Task<IStatusGeneric<int>> HardDeleteSoftDeletedEntryAsync<TEntity>(TEntity hardDeleteThisEntity, bool callSaveChanges = true)
            where TEntity : class, TInterface
        {
            if (hardDeleteThisEntity == null) throw new ArgumentNullException(nameof(hardDeleteThisEntity));
            var status = new StatusGenericHandler<int>();
            if (!_config.GetSoftDeleteValue.Compile().Invoke(hardDeleteThisEntity))
                return status.AddError($"This entry isn't {_config.TextSoftDeletedPastTense}.");

            _context.Remove(hardDeleteThisEntity);
            var numDeleted = 1;
            if (callSaveChanges)
                numDeleted = await _context.SaveChangesAsync();

            status.Message = $"Successfully {_config.TextHardDeletedPastTense} this entry";
            status.SetResult(numDeleted);
            return status;
        }

        /// <summary>
        /// This returns the soft deleted entities of type TEntity
        /// If you set up the OtherFilters property in the config then it will apply all the appropriate query filter so you only see the ones you should
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public IQueryable<TEntity> GetSoftDeletedEntries<TEntity>()
            where TEntity : class, TInterface
        {
            return _context.Set<TEntity>().IgnoreQueryFilters().Where(_config.FilterToGetValueSingleSoftDeletedEntities<TEntity, TInterface>());
        }

        //-----------------------------------------------
        //private methods

        public async Task< IStatusGeneric<int>> CheckExecuteSoftDeleteAsync<TEntity>(
            Func<TInterface, bool, Task<IStatusGeneric<int>>> softDeleteAction, params object[] keyValues)
            where TEntity : class, TInterface
        {
            var status = new StatusGenericHandler<int>();
            var entity = await _context.LoadEntityViaPrimaryKeys<TEntity>(_config.OtherFilters, true, keyValues);
            if (entity == null)
            {
                if (!_config.NotFoundIsNotAnError)
                    status.AddError("Could not find the entry you ask for.");
                return status;
            }

            return await softDeleteAction(entity, true);
        }
    }
}