using System;
using Zoo.Domain;

namespace Zoo.Application
{
    public interface IContactResponseControl
    {
        void SetContactFilter(Func<Animal, Animal, bool> shouldSuppress);
    }
}
