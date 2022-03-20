namespace OpenStatusPage.Server.Application.Cluster.Consensus
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class SnapshotApplyDataOrderAttribute : Attribute
    {
        public SnapshotApplyDataOrderAttribute(int orderIndex)
        {
            OrderIndex = orderIndex;
        }

        public int OrderIndex { get; }
    }
}
