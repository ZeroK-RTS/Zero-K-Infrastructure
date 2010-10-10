namespace PlanetWars.Utility
{
    public delegate void RunWorkerCompletedEventHandler<T>(object sender, RunWorkerCompletedEventArgs<T> e);

    public delegate void DoWorkEventHandler<TIn, TOut>(object sender, DoWorkEventArgs<TIn, TOut> e);
}