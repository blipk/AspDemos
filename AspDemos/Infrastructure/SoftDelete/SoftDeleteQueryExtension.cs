using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using AspDemos.Models;

namespace AspDemos.Infrastructure.SoftDelete {
    public enum CascadeQueryFilterTypes { CascadeSoftDelete, CascadeAndSingle }

    public static class CascadeQueryFilterExtensions {
        public static void SetCascadeQueryFilter(this IMutableEntityType entityData, CascadeQueryFilterTypes queryFilterType) {
            var methodName = $"Get{queryFilterType}Filter";
            var methodToCall = typeof(CascadeQueryFilterExtensions)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(entityData.ClrType);
            var filter = methodToCall.Invoke(null, new object[] { });

            entityData.SetQueryFilter((LambdaExpression)filter);

            if (queryFilterType == CascadeQueryFilterTypes.CascadeSoftDelete)
                entityData.AddIndex(entityData.FindProperty(nameof(ICascadeSoftDelete.SoftDeleteLevel)));

            if (queryFilterType == CascadeQueryFilterTypes.CascadeAndSingle)
                entityData.AddIndex(entityData.FindProperty(nameof(ISingleSoftDelete.SoftDeleted)));
        }

        private static LambdaExpression GetCascadeSoftDeleteFilter<TEntity>()
            where TEntity : class, ICascadeSoftDelete {
            Expression<Func<TEntity, bool>> filter = x => x.SoftDeleteLevel == 0;
            return filter;
        }

        private static LambdaExpression GetCascadeAndSingleFilter<TEntity>()
            where TEntity : class, ICascadeSoftDelete, ISingleSoftDelete {
            Expression<Func<TEntity, bool>> filter = x => x.SoftDeleteLevel == 0
                                                          && !x.SoftDeleted;
            return filter;
        }
    }
}
