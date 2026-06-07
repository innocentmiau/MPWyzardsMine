namespace Scripts.Systems.Interface
{
    public static class StatusTypeExtensions
    {
        public static string Color(this StatusType self)
        {
            switch (self)
            {
                case StatusType.ERROR:
                    return "<color=#A45756>";
                case StatusType.SUCCESS:
                    return "<color=#66A456>";
            }
            return "<color=white>";
        }
    }
}