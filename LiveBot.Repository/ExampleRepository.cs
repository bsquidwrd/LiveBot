using LiveBot.Core.Repository;
using Serilog;

namespace LiveBot.Repository
{
    public class ExampleRepository : IExampleRepository
    {
        public ExampleRepository()
        {
            Log.Information($"A new {nameof(ExampleRepository)} has been created!");
        }

        public void RepoCall()
        {
            Log.Information($"{nameof(RepoCall)} has been executed!");
        }

        public void TestCall()
        {
            Log.Information($"We did the thing!");
        }
    }
}