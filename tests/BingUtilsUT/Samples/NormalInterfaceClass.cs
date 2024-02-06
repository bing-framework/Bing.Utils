namespace BingUtilsUT.Samples;

public class NormalInterfaceClass<T> : IModelOne, IModelTwo, IModelThree<T>
{
}

public interface IModelOne { }

public interface IModelTwo { }

public interface IModelThree<T> { }

public interface IModelFour { }