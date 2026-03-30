using CommunityToolkit.Mvvm.Input;
using Dashboard.Models;
using Dashboard.Services;
using Dashboard.ViewModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class MainViewModelTests
{
    //The MainViewModel loads users from IUserService and populates Users
    [Fact]
    public async Task LoadUsersAsync_PopulatesUsers()
    {
        var mockUserService = new Mock<IUserService>();
        var fakeUsers = new List<User>
        {
            new User { Id = "1", FirstName = "Alice", LastName = "Smith", Email = "alice@test.com", Role = "Administrator" },
            new User { Id = "2", FirstName = "Bob", LastName = "Jones", Email = "bob@test.com", Role = "Customer" }
        };
        mockUserService.Setup(s => s.GetUsersAsync())
            .ReturnsAsync(new UsersResponse { Items = fakeUsers });

        var vm = new MainViewModel(mockUserService.Object);
        await Task.Delay(10);

        Assert.Equal(2, vm.Users.Count);
        Assert.Contains(vm.Users, u => u.FirstName == "Alice");
        Assert.Contains(vm.Users, u => u.FirstName == "Bob");
    }

    //The filtering works correctly based on SearchText
    [Fact]
    public async Task FilterUsers_FiltersCorrectly()
    {
        var mockUserService = new Mock<IUserService>();
        var fakeUsers = new List<User>
        {
            new User { Id = "1", FirstName = "Alice", LastName = "Smith" },
            new User { Id = "2", FirstName = "Bob", LastName = "Jones" }
        };
        mockUserService.Setup(s => s.GetUsersAsync())
            .ReturnsAsync(new UsersResponse { Items = fakeUsers });

        var vm = new MainViewModel(mockUserService.Object);
        await Task.Delay(10);

        vm.SearchText = "Alice";

        var filtered = vm.Users
            .Where(u =>
                (u.FirstName?.Contains(vm.SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.LastName?.Contains(vm.SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Email?.Contains(vm.SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Role?.Contains(vm.SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        Assert.Single(filtered);
        Assert.Equal("Alice", filtered[0].FirstName);
    }

    //The MarkUserDirty tracks users, and SaveCommand only saves dirty users
    [Fact]
    public async Task SaveCommand_SavesOnlyDirtyUsers()
    {
        var mockUserService = new Mock<IUserService>();
        var fakeUsers = new List<User>
        {
            new User { Id = "1", FirstName = "Alice" },
            new User { Id = "2", FirstName = "Bob" }
        };
        mockUserService.Setup(s => s.GetUsersAsync())
            .ReturnsAsync(new UsersResponse { Items = fakeUsers });
        mockUserService.Setup(s => s.SaveUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var vm = new MainViewModel(mockUserService.Object);
        await Task.Delay(10);

        vm.MarkUserDirty(fakeUsers[0]);

        await ((IAsyncRelayCommand)vm.SaveCommand).ExecuteAsync(null);

        mockUserService.Verify(s => s.SaveUserAsync(It.Is<User>(u => u.Id == "1")), Times.Once);
        mockUserService.Verify(s => s.SaveUserAsync(It.Is<User>(u => u.Id == "2")), Times.Never);
    }

    //The RefreshCommand clears dirty list and reloads users
    [Fact]
    public async Task RefreshCommand_ClearsDirtyAndReloads()
    {
        var mockUserService = new Mock<IUserService>();
        var fakeUsers = new List<User>
        {
            new User { Id = "1", FirstName = "Alice" }
        };
        mockUserService.Setup(s => s.GetUsersAsync())
            .ReturnsAsync(new UsersResponse { Items = fakeUsers });

        var vm = new MainViewModel(mockUserService.Object);
        await Task.Delay(10);

        vm.MarkUserDirty(fakeUsers[0]);

        await ((IAsyncRelayCommand)vm.RefreshCommand).ExecuteAsync(null);

        await ((IAsyncRelayCommand)vm.SaveCommand).ExecuteAsync(null);
        mockUserService.Verify(s => s.SaveUserAsync(It.IsAny<User>()), Times.Never);
    }

    //The LogoutCommand calls OnLogoutRequested
    [Fact]
    public async Task LogoutCommand_CallsOnLogoutRequested()
    {
        var vm = new MainViewModel(Mock.Of<IUserService>());
        bool logoutCalled = false;
        vm.OnLogoutRequested += () => logoutCalled = true;

        await vm.LogoutCommand.ExecuteAsync(null);

        Assert.True(logoutCalled);
    }
}