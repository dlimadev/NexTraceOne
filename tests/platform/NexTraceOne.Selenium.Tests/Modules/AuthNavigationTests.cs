using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para páginas públicas de autenticação.
/// Validam que login, forgot-password, reset-password, activation e MFA
/// carregam sem erros JS e renderizam os elementos esperados.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class AuthNavigationTests : SeleniumTestBase
{
    public AuthNavigationTests(BrowserFixture fixture) : base(fixture) { }

    [Fact]
    public void LoginPage_Loads_And_Shows_Form()
    {
        NavigateTo("/login");
        WaitForSuspenseComplete();

        var emailField = Driver.FindElements(By.CssSelector("input[type='email'], input[name='email'], [aria-label='Email']"));
        emailField.Should().NotBeEmpty("the login page should display an email input");

        var passwordField = Driver.FindElements(By.CssSelector("input[type='password']"));
        passwordField.Should().NotBeEmpty("the login page should display a password input");

        AssertNoJavaScriptErrors();
    }

    [Fact]
    public void ForgotPasswordPage_Loads()
    {
        AssertPageLoadsSuccessfully("/forgot-password");
    }

    [Fact]
    public void ResetPasswordPage_Loads()
    {
        AssertPageLoadsSuccessfully("/reset-password");
    }

    [Fact]
    public void ActivationPage_Loads()
    {
        AssertPageLoadsSuccessfully("/activate");
    }

    [Fact]
    public void MfaPage_Loads()
    {
        AssertPageLoadsSuccessfully("/mfa");
    }

    [Fact]
    public void TenantSelectionPage_Loads()
    {
        AssertPageLoadsSuccessfully("/select-tenant");
    }

    [Fact]
    public void UnknownRoute_Redirects_To_Root()
    {
        MockAuthSessionWithProfileIntercept();
        NavigateTo("/this-route-does-not-exist-999");
        WaitForSuspenseComplete();

        var path = new Uri(Driver.Url).AbsolutePath;
        path.Should().BeOneOf("/", "/login",
            "unknown routes should redirect to home or login");
    }
}
