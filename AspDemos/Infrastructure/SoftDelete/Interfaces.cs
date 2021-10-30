namespace AspDemos.Infrastructure.SoftDelete {
    public interface ISingleSoftDelete {
        bool SoftDeleted { get; set; }
    }

    public interface ICascadeSoftDelete {
        public byte SoftDeleteLevel { get; set; }
    }
}
