using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace VirtualRtu.Configuration.Tables
{
    public class ContainerEntity : TableEntity
    {
        private static string table;
        private static string cs;
        private string slaveJson;
        private List<Slave> slaves;

        public ContainerEntity()
        {
        }

        public ContainerEntity(string luss, string hostname, string moduleId, string virtualRtuId, string deviceId,
            List<Slave> slaves, LogLevel loggingLevel, string instrumentationKey, TimeSpan expiry, string tableName,
            string connectionString)
        {
            Luss = luss;
            Hostname = hostname;
            ModuleId = moduleId;
            VirtualRtuId = virtualRtuId;
            DeviceId = deviceId;
            Slaves = slaves;
            LoggingLevel = loggingLevel;
            InstrumentationKey = instrumentationKey;
            table = tableName;
            cs = connectionString;
            Created = DateTime.UtcNow;
            Expires = DateTime.UtcNow.Add(expiry);
        }

        /// <summary>
        ///     The Limited Use Shared Secret (LUSS)
        /// </summary>
        public string Luss
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string Hostname { get; set; }
        public string DeviceId { get; set; }

        public string ModuleId { get; set; }

        public string VirtualRtuId
        {
            get => RowKey;
            set => RowKey = value.ToLowerInvariant();
        }

        public string SlaveJson
        {
            get => slaveJson;
            set
            {
                slaveJson = value;
                slaves = JsonConvert.DeserializeObject<List<Slave>>(slaveJson);
            }
        }

        [IgnoreProperty]
        public List<Slave> Slaves
        {
            get => slaves;
            set
            {
                slaves = value;
                slaveJson = JsonConvert.SerializeObject(value);
            }
        }

        /// <summary>
        ///     Logging level
        /// </summary>
        [IgnoreProperty]
        public LogLevel LoggingLevel { get; set; }

        public string LogLevel
        {
            get => LoggingLevel.ToString();
            set => LoggingLevel = Enum.Parse<LogLevel>(value);
        }

        public string InstrumentationKey { get; set; }

        /// <summary>
        ///     The timestamp when the LUSS was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        ///     The timestamp when the LUSS will expire if not used.
        /// </summary>
        public DateTime Expires { get; set; }

        /// <summary>
        ///     The timestamp when the LUSS was received from the module.
        /// </summary>
        public DateTime? Access { get; set; }

        /// <summary>
        ///     Determines if the function returned successfully to issue token parameters.
        /// </summary>
        /// <remarks>
        ///     If the Success property is not null, then the LUSS cannot be reused.
        ///     The LUSS is intended as one time use.
        /// </remarks>
        public bool? Success { get; set; }

        public static async Task<ContainerEntity> LoadAsync(string luss, string tableName, string connectionString)
        {
            ContainerEntity entity = null;

            try
            {
                CloudStorageAccount acct = CloudStorageAccount.Parse(connectionString);
                CloudTableClient client = acct.CreateCloudTableClient();
                CloudTable cloudTable = client.GetTableReference(tableName);
                await cloudTable.CreateIfNotExistsAsync();

                TableQuery<ContainerEntity> query =
                    new TableQuery<ContainerEntity>().Where(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, luss));

                TableQuerySegment<ContainerEntity> segment =
                    await cloudTable.ExecuteQuerySegmentedAsync(query, new TableContinuationToken());

                table = tableName;
                cs = connectionString;

                if (segment == null || segment.Results.Count == 0 || segment.Results.Count > 1)
                {
                    entity = null;
                }
                else
                {
                    entity = segment.Results[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Container fault reading table.");
                Console.WriteLine($"Error - {ex.Message}");
            }

            return entity;
        }

        /// <summary>
        ///     The timestamp when a security token was reissued.
        /// </summary>
        /// <remarks>Feature TBD.</remarks>
        public async Task UpdateAsync()
        {
            CloudStorageAccount acct = CloudStorageAccount.Parse(cs);
            CloudTableClient client = acct.CreateCloudTableClient();
            CloudTable cloudTable = client.GetTableReference(table);
            await cloudTable.CreateIfNotExistsAsync();
            TableOperation operation = TableOperation.InsertOrReplace(this);
            await cloudTable.ExecuteAsync(operation);
        }
    }
}