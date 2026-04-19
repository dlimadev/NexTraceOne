using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="GraphQlDiffCalculator"/>.
/// Valida a detecção de breaking changes, mudanças aditivas e non-breaking
/// entre schemas GraphQL SDL.
/// </summary>
public sealed class GraphQlDiffCalculatorTests
{
    private const string BaseSchema = """
        type Query {
          user(id: ID!): User
          users: [User!]!
        }

        type Mutation {
          createUser(input: CreateUserInput!): User
        }

        type User {
          id: ID!
          name: String!
          email: String
        }

        input CreateUserInput {
          name: String!
          email: String
        }

        enum Status {
          ACTIVE
          INACTIVE
          PENDING
        }
        """;

    [Fact]
    public void ComputeDiff_Should_DetectRemovedType_As_Breaking()
    {
        // Arrange — remove o tipo User do schema alvo
        var targetSchema = """
            type Query {
              users: [String!]!
            }

            enum Status {
              ACTIVE
              INACTIVE
              PENDING
            }
            """;

        // Act
        var result = GraphQlDiffCalculator.ComputeDiff(BaseSchema, targetSchema);

        // Assert
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().ContainSingle(c =>
            c.ChangeType == "TypeRemoved" && c.Path == "User");
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedRootField_As_Breaking()
    {
        // Arrange — remove o campo 'user' do root type Query
        var targetSchema = """
            type Query {
              users: [User!]!
            }

            type Mutation {
              createUser(input: CreateUserInput!): User
            }

            type User {
              id: ID!
              name: String!
              email: String
            }

            input CreateUserInput {
              name: String!
              email: String
            }

            enum Status {
              ACTIVE
              INACTIVE
              PENDING
            }
            """;

        // Act
        var result = GraphQlDiffCalculator.ComputeDiff(BaseSchema, targetSchema);

        // Assert
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().ContainSingle(c =>
            c.ChangeType == "RootFieldRemoved" && c.Path == "Query" && c.Method == "user");
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedEnumValue_As_Breaking()
    {
        // Arrange — remove o valor PENDING do enum Status
        var targetSchema = """
            type Query {
              user(id: ID!): User
              users: [User!]!
            }

            type Mutation {
              createUser(input: CreateUserInput!): User
            }

            type User {
              id: ID!
              name: String!
              email: String
            }

            input CreateUserInput {
              name: String!
              email: String
            }

            enum Status {
              ACTIVE
              INACTIVE
            }
            """;

        // Act
        var result = GraphQlDiffCalculator.ComputeDiff(BaseSchema, targetSchema);

        // Assert
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().ContainSingle(c =>
            c.ChangeType == "EnumValueRemoved" && c.Path == "Status" && c.Method == "PENDING");
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedType_As_Additive()
    {
        // Arrange — adiciona novo tipo Role ao schema alvo
        var targetSchema = BaseSchema + """


            type Role {
              id: ID!
              label: String!
            }
            """;

        // Act
        var result = GraphQlDiffCalculator.ComputeDiff(BaseSchema, targetSchema);

        // Assert
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
        result.AdditiveChanges.Should().ContainSingle(c =>
            c.ChangeType == "TypeAdded" && c.Path == "Role");
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedRootField_As_Additive()
    {
        // Arrange — adiciona novo campo 'roleById' a Query
        var targetSchema = """
            type Query {
              user(id: ID!): User
              users: [User!]!
              roleById(id: ID!): Role
            }

            type Mutation {
              createUser(input: CreateUserInput!): User
            }

            type User {
              id: ID!
              name: String!
              email: String
            }

            input CreateUserInput {
              name: String!
              email: String
            }

            enum Status {
              ACTIVE
              INACTIVE
              PENDING
            }
            """;

        // Act
        var result = GraphQlDiffCalculator.ComputeDiff(BaseSchema, targetSchema);

        // Assert
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
        result.AdditiveChanges.Should().ContainSingle(c =>
            c.ChangeType == "RootFieldAdded" && c.Path == "Query" && c.Method == "roleById");
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedEnumValue_As_NonBreaking()
    {
        // Arrange — adiciona novo valor ARCHIVED ao enum Status
        var targetSchema = """
            type Query {
              user(id: ID!): User
              users: [User!]!
            }

            type Mutation {
              createUser(input: CreateUserInput!): User
            }

            type User {
              id: ID!
              name: String!
              email: String
            }

            input CreateUserInput {
              name: String!
              email: String
            }

            enum Status {
              ACTIVE
              INACTIVE
              PENDING
              ARCHIVED
            }
            """;

        // Act
        var result = GraphQlDiffCalculator.ComputeDiff(BaseSchema, targetSchema);

        // Assert
        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
        result.NonBreakingChanges.Should().ContainSingle(c =>
            c.ChangeType == "EnumValueAdded" && c.Path == "Status" && c.Method == "ARCHIVED");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_SchemasAreIdentical()
    {
        var result = GraphQlDiffCalculator.ComputeDiff(BaseSchema, BaseSchema);

        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
        result.BreakingChanges.Should().BeEmpty();
        result.AdditiveChanges.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_BothSchemasEmpty()
    {
        var result = GraphQlDiffCalculator.ComputeDiff(string.Empty, string.Empty);

        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
        result.BreakingChanges.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedField_In_NonRootType_As_Breaking()
    {
        // Arrange — remove campo 'email' do tipo User
        var targetSchema = """
            type Query {
              user(id: ID!): User
              users: [User!]!
            }

            type Mutation {
              createUser(input: CreateUserInput!): User
            }

            type User {
              id: ID!
              name: String!
            }

            input CreateUserInput {
              name: String!
              email: String
            }

            enum Status {
              ACTIVE
              INACTIVE
              PENDING
            }
            """;

        var result = GraphQlDiffCalculator.ComputeDiff(BaseSchema, targetSchema);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().ContainSingle(c =>
            c.ChangeType == "FieldRemoved" && c.Path == "User" && c.Method == "email");
    }
}
