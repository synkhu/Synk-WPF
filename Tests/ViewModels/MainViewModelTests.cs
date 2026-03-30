using CommunityToolkit.Mvvm.Input;
using Dashboard.Models;
using Dashboard.Services;
using Dashboard.ViewModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
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
        await Task.Yield();

        Assert.Equal(2, vm.Users.Count);
        Assert.Contains(vm.Users, u => u.FirstName == "Alice");
        Assert.Contains(vm.Users, u => u.FirstName == "Bob");
    }

    //The filtering works correctly based on SearchText
    [Fact]
    public void FilterUsers_FiltersCorrectly()
    {
        List<User> filtered = null!;

        var thread = new Thread(() =>
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
            Thread.Sleep(10);

            vm.SearchText = "Alice";

            filtered = vm.UsersView.Cast<User>().ToList();

            Dispatcher.ExitAllFrames();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

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
        await Task.Yield();

        vm.MarkUserDirty(fakeUsers[0]);

        await ((IAsyncRelayCommand)vm.SaveCommand).ExecuteAsync(null);

        mockUserService.Verify(s => s.SaveUserAsync(It.Is<User>(u => u.Id == "1")), Times.Once);
        mockUserService.Verify(s => s.SaveUserAsync(It.Is<User>(u => u.Id == "2")), Times.Never);
    }

    //The RefreshCommand clears dirty list and reloads users
    [Fact]
    public void RefreshCommand_ClearsDirtyAndReloads()
    {
        var thread = new Thread(() =>
        {
            var mockUserService = new Mock<IUserService>();
            var fakeUsers = new List<User>
        {
            new User { Id = "1", FirstName = "Alice" }
        };
            mockUserService.Setup(s => s.GetUsersAsync())
                .ReturnsAsync(new UsersResponse { Items = fakeUsers });
            mockUserService.Setup(s => s.SaveUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var vm = new MainViewModel(mockUserService.Object);
            Thread.Sleep(10);

            vm.MarkUserDirty(fakeUsers[0]);

            ((IAsyncRelayCommand)vm.RefreshCommand).ExecuteAsync(null).Wait();

            ((IAsyncRelayCommand)vm.SaveCommand).ExecuteAsync(null).Wait();

            mockUserService.Verify(s => s.SaveUserAsync(It.IsAny<User>()), Times.Never);

            Assert.Single(vm.UsersView.Cast<User>());
            Assert.Equal("Alice", vm.UsersView.Cast<User>().First().FirstName);

            Dispatcher.ExitAllFrames();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
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