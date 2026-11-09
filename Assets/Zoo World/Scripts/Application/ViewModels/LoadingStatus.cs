namespace Zoo.Application
{
    public readonly struct LoadingStatus
    {
        public readonly bool Visible;
        public readonly string Stage;
        public readonly string Error;
        public readonly float Progress;

        public LoadingStatus(bool visible, string stage, float progress, string error = null)
        {
            Visible = visible;
            Stage = stage;
            Progress = progress;
            Error = error;
        }
    }
}
