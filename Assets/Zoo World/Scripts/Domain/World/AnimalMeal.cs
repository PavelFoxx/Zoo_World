namespace Zoo.Domain
{
    public readonly struct AnimalMeal
    {
        public readonly Animal Predator;
        public readonly Entity Victim;
        public readonly string Message;

        public AnimalMeal(Animal predator, Entity victim, string message = null)
        {
            Predator = predator;
            Victim = victim;
            Message = message;
        }
    }
}
