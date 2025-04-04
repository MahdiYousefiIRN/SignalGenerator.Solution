
using SignalGenerator.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Interfaces
{
    public interface IProtocolDataStore
    {
        /// <summary>
        /// Sends a signal using the specified protocol.
        /// </summary>
        /// <param name="signal">The signal to send.</param>
        /// <param name="protocolType">The type of protocol to use.</param>
        /// <returns>A boolean indicating whether the signal was sent successfully.</returns>
        Task<bool> SendSignalAsync(SignalData signal, string protocolType);

        /// <summary>
        /// Retrieves a signal by its ID.
        /// </summary>
        /// <param name="id">The ID of the signal to retrieve.</param>
        /// <returns>The retrieved SignalData object, or null if not found.</returns>
        Task<SignalData?> GetSignalAsync(string id);

        /// <summary>
        /// Retrieves signals based on the specified criteria.
        /// </summary>
        /// <param name="userId">The ID of the user to filter by.</param>
        /// <param name="startTime">The start time for the signal range.</param>
        /// <param name="endTime">The end time for the signal range.</param>
        /// <param name="protocolType">Optional protocol type to filter by.</param>
        /// <returns>A list of SignalData objects matching the criteria.</returns>
        Task<List<SignalData>> GetSignalsAsync(string userId, DateTime? startTime = null, DateTime? endTime = null, string? protocolType = null);

        /// <summary>
        /// Deletes a signal by its ID.
        /// </summary>
        /// <param name="id">The ID of the signal to delete.</param>
        /// <returns>A boolean indicating whether the signal was deleted successfully.</returns>
        Task<bool> DeleteSignalAsync(string id);

        /// <summary>
        /// Updates an existing signal.
        /// </summary>
        /// <param name="signal">The updated signal data.</param>
        /// <returns>A boolean indicating whether the signal was updated successfully.</returns>
        Task<bool> UpdateSignalAsync(SignalData signal);

        /// <summary>
        /// Saves a list of signals asynchronously.
        /// </summary>
        /// <param name="signals">The list of signals to save.</param>
        /// <returns>A boolean indicating whether the signals were saved successfully.</returns>
        Task<bool> SaveSignalsAsync(List<SignalData> signals);
    }
}
