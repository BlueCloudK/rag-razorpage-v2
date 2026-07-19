using System.Threading.Tasks;
using ServiceLayer.Dtos;

namespace ServiceLayer.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginInput input);
        Task<AuthResult> RegisterStudentAsync(RegisterInput input);
        Task LogoutAsync();
    }
}
