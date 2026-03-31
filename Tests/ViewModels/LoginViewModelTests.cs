using System;
using System.Threading.Tasks;
using Dashboard.Services;
using Dashboard.ViewModels;
using Moq;
using Xunit;

public class LoginViewModelTests
{
    //When the email or password field is empty the ViewModel shows the proper error message.
    [Fact]
    public async Task LoginCommand_EmptyEmailOrPassword_ShowsError()
    {
        var mockAuth = new Mock<IAuthService>();
        var vm = new LoginViewModel(mockAuth.Object);
        string? error = null;
        vm.ShowErrorMessage += msg => error = msg;

        vm.Email = "";
        vm.Password = "";

        await ((CommunityToolkit.Mvvm.Input.AsyncRelayCommand)vm.LoginCommand).ExecuteAsync(null);

        Assert.Equal("Please enter both email and password.", error);
    }

    //When the login fails the ViewModel shows the error message returned from the AuthService.
    [Fact]
    public async Task LoginCommand_LoginFails_ShowsError()
    {
        var mockAuth = new Mock<IAuthService>();
        mockAuth.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((false, "Invalid credentials"));
        mockAuth.Setup(a => a.CheckAdminAsync()).ReturnsAsync((true, null));

        var vm = new LoginViewModel(mockAuth.Object);
        string? error = null;
        vm.ShowErrorMessage += msg => error = msg;

        vm.Email = "any@email.com";
        vm.Password = "anyPassword";

        await ((CommunityToolkit.Mvvm.Input.AsyncRelayCommand)vm.LoginCommand).ExecuteAsync(null);

        Assert.Equal("Invalid credentials", error);
    }

    //When the login is successful and the user is an admin the OnLoginSucceeded event is called.
    [Fact]
    public async Task LoginCommand_LoginSucceedsAndAdmin_CallsOnLoginSucceeded()
    {
        var mockAuth = new Mock<IAuthService>();
        mockAuth.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((true, null));
        mockAuth.Setup(a => a.CheckAdminAsync()).ReturnsAsync((true, null));

        var vm = new LoginViewModel(mockAuth.Object);
        bool succeeded = false;
        vm.OnLoginSucceeded += () => succeeded = true;

        vm.Email = "admin@test.com";
        vm.Password = "pass";

        await ((CommunityToolkit.Mvvm.Input.AsyncRelayCommand)vm.LoginCommand).ExecuteAsync(null);

        Assert.True(succeeded);
    }

    //The IsLoggingIn is true while the login is in progress and false after it completes.
    [Fact]
    public async Task LoginCommand_SetsIsLoggingInCorrectly()
    {
        var mockAuth = new Mock<IAuthService>();
        mockAuth.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(async () => { await Task.Delay(10); return (true, null); });
        mockAuth.Setup(a => a.CheckAdminAsync()).ReturnsAsync((true, null));

        var vm = new LoginViewModel(mockAuth.Object);
        bool isLoggingInDuringExecution = false;

        vm.Email = "test@test.com";
        vm.Password = "pass";

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsLoggingIn) && vm.IsLoggingIn)
                isLoggingInDuringExecution = true;
        };

        await ((CommunityToolkit.Mvvm.Input.AsyncRelayCommand)vm.LoginCommand).ExecuteAsync(null);

        Assert.True(isLoggingInDuringExecution);
        Assert.False(vm.IsLoggingIn);
    }
}