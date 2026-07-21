using System.Diagnostics.Tracing;
using FluentAssertions;
using MailSender.Application.Settings;
using MailSender.Infrastructure.Registration;
using Microsoft.Extensions.Options;

namespace MailSender.UnitTests.Registration;

public class RegistrationPasswordValidatorTests
{
[Fact]
public void IsValid_WhenPasswordMatchesConfiguredStudent_ReturnsTrue()
{
    // Arrange
    var students = new List<StudentSettings>
    {
        new()
        {
            Surname = "Haberka",
            IndexSuffix = "13"
        }
    };

    var options = Options.Create(students);
    var validator = new RegistrationPasswordValidator(options);

    // Act
    var result = validator.IsValid("dwa13");

    // Assert
    result.Should().BeTrue();
}
[Fact]
public void IsValid_WhenPasswordDoesNotMatchConfiguredStudent_ReturnsFalse()
{
    // Arrange
    var students = new List<StudentSettings>
    {
        new()
        {
            Surname = "Haberka",
            IndexSuffix = "13"
        }
    };

    var options = Options.Create(students);
    var validator = new RegistrationPasswordValidator(options);

    // Act
    var result = validator.IsValid("wrong-password");

    // Assert
    result.Should().BeFalse();
}
[Fact]
public void IsValid_WhenPasswordIsEmpty_ReturnsFalse()
    {
        // Arrange 
        var students = new List<StudentSettings>
        {
            new()
            {
                Surname = "Haberka",
                IndexSuffix = "13"
            }
        };
        var options = Options.Create(students);
        var validator = new RegistrationPasswordValidator(options);

        // Act
        var result = validator.IsValid("");

        // Assert
        result.Should().BeFalse();
    }
    [Fact]
public void IsValid_WhenStudentsListIsEmpty_ReturnsFalse()
    {
        // Arrange 
        var studnets = new List<StudentSettings>();
        var options = Options.Create(studnets);
        var validator = new RegistrationPasswordValidator(options);

        // Act
        var result = validator.IsValid("dwa13");

        // Assert
        result.Should().BeFalse();
    }
    [Fact]
public void GetInvalidPasswordMessage_WhenStudentsExist_ReturnsExpectedMessage()
    {
        // Arrange
        var students = new List<StudentSettings>
        {
            new()
            {
                Surname = "Lewandowski",
                IndexSuffix = "9"
            },
            new()
            {
                Surname = "Haberka",
                IndexSuffix = "13"
            }
            
        };
        var options = Options.Create(students);
        var validator = new RegistrationPasswordValidator(options);
        var expected = "Invalid index-based password. Expected one of suffixes: 9, 13";
        // Act
        var result = validator.GetInvalidPasswordMessage();
        // Assert
        result.Should().Be(expected);   
    }
}