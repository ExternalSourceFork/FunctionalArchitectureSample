
namespace Architecture.DataSource.MongoDb.Tests.Todo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Architecture.DataSource.MongoDb.Todo;
    using Architecture.Domain.Common.Database;

    using LanguageExt.UnitTesting;

    using MongoDB.Driver;

    using Moq;

    using Shouldly;

    using Xunit;

    public class TodoItemDataSource_GetAllAsyncTest
    {
        private readonly MockRepository _mockRepository;
        private readonly Mock<IMongoClient> _mockMongoClient;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoCollection<TodoItemDto>> _mockCollection;
        private readonly Mock<IAsyncCursor<TodoItemDto>> _mockAsyncCursor;

        public TodoItemDataSource_GetAllAsyncTest()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);

            _mockMongoClient = _mockRepository.Create<IMongoClient>();
            _mockDatabase = _mockRepository.Create<IMongoDatabase>();
            _mockCollection = _mockRepository.Create<IMongoCollection<TodoItemDto>>();
            _mockAsyncCursor = _mockRepository.Create<IAsyncCursor<TodoItemDto>>();
        }

        private TodoItemDataSource CreateService()
        {
            _mockDatabase
                .Setup(x => x.GetCollection<TodoItemDto>("items", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockCollection.Object);

            _mockMongoClient
                .Setup(x => x.GetDatabase("todo", It.IsAny<MongoDatabaseSettings>()))
                .Returns(_mockDatabase.Object);

            return new(_mockMongoClient.Object);
        }

        [Trait("Todo", "GetAllAsync")]
        [Fact(DisplayName = "With data source returning collection should return Right with that collection")]
        public async Task GetAllAsync_WithDataSourceReturningCollection_ShouldReturnRightWithThatCollection()
        {
            // Arrange
            var expected = new List<TodoItemDto> { 
                new TodoItemDto(Guid.NewGuid(), false, "Test 1")
            };

            TestHelper.MockAsyncCursor(_mockAsyncCursor, expected);

            _mockCollection
                .Setup(svc =>
                    svc.FindAsync(
                        It.IsAny<ExpressionFilterDefinition<TodoItemDto>>(),
                        It.IsAny<FindOptions<TodoItemDto, TodoItemDto>>(),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_mockAsyncCursor.Object));

            var dataSource = CreateService();

            // Act
            var actual = dataSource.GetAllAsync();

            // Assert
            await actual.ShouldBeRight();
            await actual.ShouldBeRight(dtos => dtos.ShouldHaveSingleItem());
            await actual.ShouldBeRight(dtos => dtos[0].ShouldBe(expected[0]));
        }

        [Trait("Todo", "GetAllAsync")]
        [Fact(DisplayName = "With data source throwing exception should return Left Retrieve failure and thrown exception")]
        public async Task GetAllAsync_WithDataSourceThrowingException_ShouldReturnLeftRetrieveFailueAndThrownException()
        {
            // Arrange
            var exception = new Exception("Test exception");
            _mockCollection
                .Setup(svc =>
                    svc.FindAsync(
                        It.IsAny<ExpressionFilterDefinition<TodoItemDto>>(),
                        It.IsAny<FindOptions<TodoItemDto, TodoItemDto>>(),
                        It.IsAny<CancellationToken>()))
                .Throws(exception);

            var dataSource = CreateService();

            // Act
            var actual = dataSource.GetAllAsync();

            // Assert
            await actual.ShouldBeLeft(failure => failure.ShouldBeOfType<DatabaseFailure.Retrieve>());
            await actual.ShouldBeLeft(failure => failure.Error.Exception.ShouldBeSome());
            await actual.ShouldBeLeft(failure => failure.Error.Exception.ShouldBe(exception));
        }
    }
}
