using System;
using System.Collections.Generic;
using FormFlow.State;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace FormFlow.Tests;

public class JourneyInstanceTests
{
    [Fact]
    public void Delete_CallsDeleteOnStateProvider()
    {
        // Arrange
        var instanceId = new JourneyInstanceId("instance", new Dictionary<string, StringValues>());

        var stateProvider = new Mock<IUserInstanceStateProvider>();

        var journeyName = "journey";
        var stateType = typeof(MyState);

        var instance = (JourneyInstance<MyState>)JourneyInstance.Create(
            stateProvider.Object,
            journeyName,
            instanceId,
            stateType,
            new MyState(),
            properties: new Dictionary<object, object>());

        var newState = new MyState();

        // Act
        instance.Complete();

        // Assert
        stateProvider.Verify(mock => mock.CompleteInstance(journeyName, instanceId, stateType));
    }

    [Fact]
    public void UpdateState_DeletedInstance_ThrowsInvalidOperationException()
    {
        // Arrange
        var instanceId = new JourneyInstanceId("instance", new Dictionary<string, StringValues>());

        var stateProvider = new Mock<IUserInstanceStateProvider>();

        var journeyName = "journey";
        var stateType = typeof(MyState);

        var instance = (JourneyInstance<MyState>)JourneyInstance.Create(
            stateProvider.Object,
            journeyName,
            instanceId,
            stateType,
            new MyState(),
            properties: new Dictionary<object, object>());

        var newState = new MyState();

        instance.Complete();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => instance.UpdateState(newState));
    }

    [Fact]
    public void UpdateState_CallsUpdateStateOnStateProvider()
    {
        // Arrange
        var instanceId = new JourneyInstanceId("instance", new Dictionary<string, StringValues>());

        var stateProvider = new Mock<IUserInstanceStateProvider>();

        var journeyName = "journey";
        var stateType = typeof(MyState);

        var instance = (JourneyInstance<MyState>)JourneyInstance.Create(
            stateProvider.Object,
            journeyName,
            instanceId,
            stateType,
            new MyState(),
            properties: new Dictionary<object, object>());

        var newState = new MyState();

        // Act
        instance.UpdateState(newState);

        // Assert
        stateProvider.Verify(mock => mock.UpdateInstanceState(journeyName, instanceId, stateType, newState));
        Assert.Same(newState, instance.State);
    }

    public class MyState { }
}
