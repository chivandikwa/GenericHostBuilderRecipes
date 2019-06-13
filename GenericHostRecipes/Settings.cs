using FluentValidation;

namespace GenericHostBuilderRecipes
{
    public class Settings
    {
        public string Sample { get; set; }
    }

    public class SettingsValidator: AbstractValidator<Settings>
    {
        public SettingsValidator()
        {
            RuleFor(settings => settings).NotNull();

            RuleFor(settings => settings.Sample)
                .NotEmpty()
                .NotNull();
        }
    }

    public static class SettingsExtensions
    {
        public static void Validate(this Settings productWebServiceSettings)
        {
            new SettingsValidator()
                .ValidateAndThrow(productWebServiceSettings);
        }
    }
}