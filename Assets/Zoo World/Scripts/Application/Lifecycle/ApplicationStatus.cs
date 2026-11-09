namespace Zoo.Application
{
    public readonly struct ApplicationStatus
    {
        public ApplicationStage Stage { get; }

        public float SceneProgress { get; }

        public string Error { get; }

        public ApplicationStatus(ApplicationStage stage, float sceneProgress = 0, string error = null)
        {
            Stage = stage;
            SceneProgress = sceneProgress;
            Error = error;
        }
    }
}
