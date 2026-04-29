using KATCRUDServices.Core.Interfaces;
using Npgsql;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace KATCRUDServices.Core.Services
{
    /// <summary>
    /// Static utility class for generating auto-numbered values based on sequence patterns
    /// Gets next sequence from PostgreSQL database sequences
    /// </summary>
    public static class AutoNumberService
    {
        /// <summary>
        /// Generates a formatted auto-number based on the sequence pattern
        /// </summary>
        /// <param name="sequencePattern">Pattern string like |SeqName:CASE-{0:00000}|</param>
        /// <param name="dataProvider">Data provider to get sequence from database</param>
        /// <returns>Formatted auto-number string</returns>
        public static async Task<string> GenerateAutoNumberAsync(string sequencePattern, IDataProvider dataProvider)
        {
            try
            {
                // Parse pattern: |SeqName:pattern|
                var (sequenceName, pattern) = ParseSequencePattern(sequencePattern);

                if (string.IsNullOrEmpty(sequenceName) || string.IsNullOrEmpty(pattern))
                {
                    throw new InvalidOperationException($"Invalid sequence pattern format: {sequencePattern}");
                }

                // Get next sequence number from database
                var nextSequence = await GetNextSequenceAsync(sequenceName, dataProvider);

                // Format the auto-number
                return FormatAutoNumber(pattern, nextSequence);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating auto-number for pattern {sequencePattern}", ex);
            }
        }

        /// <summary>
        /// Gets the next sequence number from PostgreSQL database
        /// </summary>
        /// <param name="sequenceName">Name of the sequence in database</param>
        /// <param name="dataProvider">Data provider to execute query</param>
        /// <returns>Next sequence number</returns>
        public static async Task<long> GetNextSequenceAsync(string sequenceName, IDataProvider dataProvider)
        {
            try
            {
                // For PostgreSQL: SELECT NEXTVAL('sequence_name')
                // For SQL Server: SELECT NEXT VALUE FOR sequence_name
                var query = $"SELECT NEXTVAL('{sequenceName}')";

                var connection = await dataProvider.GetConnectionAsync();
                var transaction = await dataProvider.BeginTransactionAsync(connection);

                try
                {
                    var result = await dataProvider.ExecuteScalarAsync(query, null, transaction);

                    transaction.Commit();

                    if (result == null || result == DBNull.Value)
                    {
                        throw new InvalidOperationException($"Failed to get next sequence value for {sequenceName}");
                    }

                    if (long.TryParse(result.ToString(), out var sequenceValue))
                    {
                        return sequenceValue;
                    }

                    throw new InvalidOperationException($"Invalid sequence value returned: {result}");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    transaction.Dispose();
                    connection.Close();
                    connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                // Surface connection failures clearly so they are easier to diagnose
                if (IsConnectionFailure(ex))
                    throw new InvalidOperationException(
                        $"Cannot connect to database while getting next sequence for {sequenceName}. Check that the database server is reachable and connection string is correct. Inner error: {ex.Message}", ex);
                throw new InvalidOperationException($"Error getting next sequence for {sequenceName}", ex);
            }
        }

        private static bool IsConnectionFailure(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is SocketException)
                    return true;
                if (e is NpgsqlException)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the current sequence value without incrementing
        /// </summary>
        /// <param name="sequenceName">Name of the sequence in database</param>
        /// <param name="dataProvider">Data provider to execute query</param>
        /// <returns>Current sequence number</returns>
        public static async Task<long> GetCurrentSequenceAsync(string sequenceName, IDataProvider dataProvider)
        {
            try
            {
                // For PostgreSQL: SELECT CURRVAL('sequence_name')
                var query = $"SELECT CURRVAL('{sequenceName}')";

                var connection = await dataProvider.GetConnectionAsync();
                var transaction = await dataProvider.BeginTransactionAsync(connection);

                try
                {
                    var result = await dataProvider.ExecuteScalarAsync(query, null, transaction);

                    transaction.Commit();

                    if (result == null || result == DBNull.Value)
                    {
                        return 0;
                    }

                    if (long.TryParse(result.ToString(), out var sequenceValue))
                    {
                        return sequenceValue;
                    }

                    return 0;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    transaction.Dispose();
                    connection.Close();
                    connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting current sequence for {sequenceName}", ex);
            }
        }

        /// <summary>
        /// Resets the sequence counter in database
        /// </summary>
        /// <param name="sequenceName">Name of the sequence in database</param>
        /// <param name="dataProvider">Data provider to execute query</param>
        public static async Task ResetSequenceAsync(string sequenceName, IDataProvider dataProvider)
        {
            try
            {
                // For PostgreSQL: ALTER SEQUENCE sequence_name RESTART WITH 1
                var query = $"ALTER SEQUENCE {sequenceName} RESTART WITH 1";

                var connection = await dataProvider.GetConnectionAsync();
                var transaction = await dataProvider.BeginTransactionAsync(connection);

                try
                {
                    await dataProvider.ExecuteNonQueryAsync(query, null, transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    transaction.Dispose();
                    connection.Close();
                    connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error resetting sequence {sequenceName}", ex);
            }
        }

        /// <summary>
        /// Parses sequence pattern in format: |SeqName:pattern|
        /// </summary>
        public static (string sequenceName, string pattern) ParseSequencePattern(string sequencePattern)
        {
            // Pattern format: |SeqName:pattern|
            var match = Regex.Match(sequencePattern, @"\|([^:]+):(.+)\|");

            if (!match.Success || match.Groups.Count < 3)
            {
                return (string.Empty, string.Empty);
            }

            var sequenceName = match.Groups[1].Value.Trim();
            var pattern = match.Groups[2].Value.Trim();

            return (sequenceName, pattern);
        }

        /// <summary>
        /// Formats auto-number using the pattern and sequence number
        /// Supports placeholders: {0:format} for sequence, {1:format} for date
        /// </summary>
        public static string FormatAutoNumber(string pattern, long sequenceNumber)
        {
            try
            {
                string result;

                // Check if pattern contains date placeholder {1
                if (pattern.Contains("{1"))
                {
                    // Extract date format from pattern if specified
                    var dateFormatMatch = Regex.Match(pattern, @"\{1:([^}]+)\}");
                    var dateFormat = dateFormatMatch.Success ? dateFormatMatch.Groups[1].Value : "ddMMyyyy";

                    result = string.Format(pattern, sequenceNumber, DateTime.Now.ToString(dateFormat));
                }
                else
                {
                    result = string.Format(pattern, sequenceNumber);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error formatting auto-number with pattern {pattern}", ex);
            }
        }


    }
}
