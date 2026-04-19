using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="ProtobufDiffCalculator"/>.
/// Valida a detecção de breaking changes, mudanças aditivas e non-breaking
/// entre ficheiros Protocol Buffers (.proto), incluindo regras de wire format.
/// </summary>
public sealed class ProtobufDiffCalculatorTests
{
    private const string BaseProto = """
        syntax = "proto3";

        package userservice;

        message User {
          int64 id = 1;
          string name = 2;
          string email = 3;
        }

        message CreateUserRequest {
          string name = 1;
          string email = 2;
        }

        message CreateUserResponse {
          User user = 1;
        }

        service UserService {
          rpc GetUser(GetUserRequest) returns (User);
          rpc CreateUser(CreateUserRequest) returns (CreateUserResponse);
        }

        message GetUserRequest {
          int64 id = 1;
        }

        enum Status {
          STATUS_UNKNOWN = 0;
          ACTIVE = 1;
          INACTIVE = 2;
        }
        """;

    [Fact]
    public void ComputeDiff_Should_DetectRemovedMessage_As_Breaking()
    {
        // Arrange — remove a message CreateUserRequest
        var targetProto = """
            syntax = "proto3";

            message User {
              int64 id = 1;
              string name = 2;
              string email = 3;
            }

            service UserService {
              rpc GetUser(GetUserRequest) returns (User);
            }

            message GetUserRequest {
              int64 id = 1;
            }

            enum Status {
              STATUS_UNKNOWN = 0;
              ACTIVE = 1;
              INACTIVE = 2;
            }
            """;

        var result = ProtobufDiffCalculator.ComputeDiff(BaseProto, targetProto);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c =>
            c.ChangeType == "MessageRemoved" && c.Path == "CreateUserRequest");
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedRpc_As_Breaking()
    {
        // Arrange — remove o RPC CreateUser
        var targetProto = """
            syntax = "proto3";

            message User {
              int64 id = 1;
              string name = 2;
              string email = 3;
            }

            message GetUserRequest {
              int64 id = 1;
            }

            message CreateUserRequest {
              string name = 1;
              string email = 2;
            }

            message CreateUserResponse {
              User user = 1;
            }

            service UserService {
              rpc GetUser(GetUserRequest) returns (User);
            }

            enum Status {
              STATUS_UNKNOWN = 0;
              ACTIVE = 1;
              INACTIVE = 2;
            }
            """;

        var result = ProtobufDiffCalculator.ComputeDiff(BaseProto, targetProto);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().ContainSingle(c =>
            c.ChangeType == "RpcRemoved" && c.Path == "UserService" && c.Method == "CreateUser");
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedField_As_Breaking()
    {
        // Arrange — remove campo 'email' (field 3) da message User
        var targetProto = """
            syntax = "proto3";

            message User {
              int64 id = 1;
              string name = 2;
            }

            message CreateUserRequest {
              string name = 1;
              string email = 2;
            }

            message CreateUserResponse {
              User user = 1;
            }

            service UserService {
              rpc GetUser(GetUserRequest) returns (User);
              rpc CreateUser(CreateUserRequest) returns (CreateUserResponse);
            }

            message GetUserRequest {
              int64 id = 1;
            }

            enum Status {
              STATUS_UNKNOWN = 0;
              ACTIVE = 1;
              INACTIVE = 2;
            }
            """;

        var result = ProtobufDiffCalculator.ComputeDiff(BaseProto, targetProto);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().ContainSingle(c =>
            c.ChangeType == "FieldRemoved" && c.Path == "User" && c.Method == "email");
    }

    [Fact]
    public void ComputeDiff_Should_DetectFieldNumberReuse_As_Breaking()
    {
        // Arrange — reutiliza o field number 2 (era 'name') para 'nickname' na message User
        var targetProto = """
            syntax = "proto3";

            message User {
              int64 id = 1;
              string nickname = 2;
              string email = 3;
            }

            message CreateUserRequest {
              string name = 1;
              string email = 2;
            }

            message CreateUserResponse {
              User user = 1;
            }

            service UserService {
              rpc GetUser(GetUserRequest) returns (User);
              rpc CreateUser(CreateUserRequest) returns (CreateUserResponse);
            }

            message GetUserRequest {
              int64 id = 1;
            }

            enum Status {
              STATUS_UNKNOWN = 0;
              ACTIVE = 1;
              INACTIVE = 2;
            }
            """;

        var result = ProtobufDiffCalculator.ComputeDiff(BaseProto, targetProto);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c =>
            c.ChangeType == "FieldNumberReused" && c.Path == "User");
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedEnumValue_As_Breaking()
    {
        // Arrange — remove valor INACTIVE do enum Status
        var targetProto = """
            syntax = "proto3";

            message User {
              int64 id = 1;
              string name = 2;
              string email = 3;
            }

            message CreateUserRequest {
              string name = 1;
              string email = 2;
            }

            message CreateUserResponse {
              User user = 1;
            }

            service UserService {
              rpc GetUser(GetUserRequest) returns (User);
              rpc CreateUser(CreateUserRequest) returns (CreateUserResponse);
            }

            message GetUserRequest {
              int64 id = 1;
            }

            enum Status {
              STATUS_UNKNOWN = 0;
              ACTIVE = 1;
            }
            """;

        var result = ProtobufDiffCalculator.ComputeDiff(BaseProto, targetProto);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().ContainSingle(c =>
            c.ChangeType == "EnumValueRemoved" && c.Path == "Status" && c.Method == "INACTIVE");
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedMessage_As_Additive()
    {
        // Arrange — adiciona nova message UpdateUserRequest
        var targetProto = BaseProto + """

            message UpdateUserRequest {
              int64 id = 1;
              string name = 2;
            }
            """;

        var result = ProtobufDiffCalculator.ComputeDiff(BaseProto, targetProto);

        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
        result.AdditiveChanges.Should().ContainSingle(c =>
            c.ChangeType == "MessageAdded" && c.Path == "UpdateUserRequest");
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedEnumValue_As_NonBreaking()
    {
        // Arrange — adiciona novo valor SUSPENDED ao enum Status
        var targetProto = """
            syntax = "proto3";

            message User {
              int64 id = 1;
              string name = 2;
              string email = 3;
            }

            message CreateUserRequest {
              string name = 1;
              string email = 2;
            }

            message CreateUserResponse {
              User user = 1;
            }

            service UserService {
              rpc GetUser(GetUserRequest) returns (User);
              rpc CreateUser(CreateUserRequest) returns (CreateUserResponse);
            }

            message GetUserRequest {
              int64 id = 1;
            }

            enum Status {
              STATUS_UNKNOWN = 0;
              ACTIVE = 1;
              INACTIVE = 2;
              SUSPENDED = 3;
            }
            """;

        var result = ProtobufDiffCalculator.ComputeDiff(BaseProto, targetProto);

        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
        result.NonBreakingChanges.Should().ContainSingle(c =>
            c.ChangeType == "EnumValueAdded" && c.Path == "Status" && c.Method == "SUSPENDED");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_ProtosAreIdentical()
    {
        var result = ProtobufDiffCalculator.ComputeDiff(BaseProto, BaseProto);

        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
        result.BreakingChanges.Should().BeEmpty();
        result.AdditiveChanges.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_BothProtoContentsEmpty()
    {
        var result = ProtobufDiffCalculator.ComputeDiff(string.Empty, string.Empty);

        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
        result.BreakingChanges.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedService_As_Breaking()
    {
        // Arrange — remove UserService
        var targetProto = """
            syntax = "proto3";

            message User {
              int64 id = 1;
              string name = 2;
              string email = 3;
            }

            message GetUserRequest {
              int64 id = 1;
            }

            enum Status {
              STATUS_UNKNOWN = 0;
              ACTIVE = 1;
              INACTIVE = 2;
            }
            """;

        var result = ProtobufDiffCalculator.ComputeDiff(BaseProto, targetProto);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c =>
            c.ChangeType == "ServiceRemoved" && c.Path == "UserService");
    }
}
