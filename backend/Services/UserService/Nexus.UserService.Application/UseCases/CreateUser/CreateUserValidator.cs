using FluentValidation;

namespace Nexus.UserService.Application.UseCases.CreateUser;

/// <summary>
/// Validator cho CreateUserCommand dùng FluentValidation.
/// Validation chạy trước khi vào handler (qua MediatR pipeline behavior),
/// nên handler không cần check input nữa - keep it clean.
/// </summary>
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .MaximumLength(256).WithMessage("Email không được quá 256 ký tự.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được quá 100 ký tự.");

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\d{9,11}$").WithMessage("Số điện thoại phải từ 9 đến 11 chữ số.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.IdentifyNumber), () =>
        {
            RuleFor(x => x.IdentifyNumber)
                .MaximumLength(20).WithMessage("Số CMND/CCCD không được quá 20 ký tự.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Gender), () =>
        {
            RuleFor(x => x.Gender)
                .Must(g => new[] { "Male", "Female", "Other" }.Contains(g))
                .WithMessage("Giới tính phải là Male, Female hoặc Other.");
        });

        When(x => x.DateOfBirth.HasValue && x.DateOfBirth.Value != default, () =>
        {
            RuleFor(x => x.DateOfBirth!.Value)
                .LessThan(DateTime.UtcNow.AddYears(-10)).WithMessage("Ngày sinh không hợp lệ (phải từ 10 tuổi trở lên).");
        });

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");
    }
}
