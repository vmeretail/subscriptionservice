namespace Core3WorkerServiceExample
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// 
    /// </summary>
    public class Program
    {
        #region Methods

        /// <summary>
        /// Creates the host builder.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(String[] args)
        {
            return Host.CreateDefaultBuilder(args).ConfigureServices((hostContext,
                                                                      services) =>
                                                                     {
                                                                         services.AddHostedService<Worker>();
                                                                     });
        }

        /// <summary>
        /// Mains the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(String[] args)
        {
            Program.CreateHostBuilder(args).Build().Run();
        }

        #endregion
    }
}