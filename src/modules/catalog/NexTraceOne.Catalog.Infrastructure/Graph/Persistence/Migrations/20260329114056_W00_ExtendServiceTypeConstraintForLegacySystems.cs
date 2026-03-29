using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W00_ExtendServiceTypeConstraintForLegacySystems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cat_service_assets_service_type",
                table: "cat_service_assets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cat_service_assets_service_type",
                table: "cat_service_assets",
                sql: "\"ServiceType\" IN ('RestApi', 'SoapService', 'KafkaProducer', 'KafkaConsumer', 'BackgroundService', 'ScheduledProcess', 'IntegrationComponent', 'SharedPlatformService', 'GraphqlApi', 'GrpcService', 'LegacySystem', 'Gateway', 'ThirdParty', 'CobolProgram', 'CicsTransaction', 'ImsTransaction', 'BatchJob', 'MainframeSystem', 'MqQueueManager', 'ZosConnectApi')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cat_service_assets_service_type",
                table: "cat_service_assets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cat_service_assets_service_type",
                table: "cat_service_assets",
                sql: "\"ServiceType\" IN ('RestApi', 'GraphqlApi', 'GrpcService', 'KafkaProducer', 'KafkaConsumer', 'BackgroundService', 'LegacySystem', 'Gateway', 'ThirdParty')");
        }
    }
}
