namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ductus.FluentDocker.Model.Containers;
    using Ductus.FluentDocker.Services;

    /// <summary>
    /// 
    /// </summary>
    public static class DockerExtension
    {
        #region Methods

        /// <summary>
        /// ClearUpContainer the container.
        /// </summary>
        /// <param name="containerService">The container service.</param>
        public static void ClearUpContainer(this IContainerService containerService)
        {
            IList<IVolumeService> volumes = new List<IVolumeService>();
            IList<INetworkService> networks = containerService.GetNetworks();

            foreach (INetworkService networkService in networks)
            {
                networkService.Detatch(containerService, true);
            }

            // Doing a direct call to .GetVolumes throws an exception if there aren't any so we need to check first :|
            IDictionary<String, VolumeMount> configurationVolumes = containerService.GetConfiguration(true).Config.Volumes;
            if (configurationVolumes != null && configurationVolumes.Any())
            {
                volumes = containerService.GetVolumes();
            }

            containerService.StopOnDispose = true;
            containerService.RemoveOnDispose = true;
            containerService.Dispose();

            foreach (IVolumeService volumeService in volumes)
            {
                volumeService.Stop();
                volumeService.Remove(true);
                volumeService.Dispose();
            }
        }

        #endregion
    }
}