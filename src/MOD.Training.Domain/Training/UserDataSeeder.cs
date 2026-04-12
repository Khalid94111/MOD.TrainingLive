using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

public class MyDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IGuidGenerator _guidGenerator;

    public MyDataSeedContributor(
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IGuidGenerator guidGenerator)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var employees = new[]
        {
            new { ServiceNumber = "D1-7443", NameEn = "Ahmed Mohammed Al Amri",    Email = "ahmed.amri@example.com",    UserName = "D1-7443"  },
            new { ServiceNumber = "D1-7446", NameEn = "Salem Saeed Al Balushi",    Email = "salem.balushi@example.com", UserName = "D1-7446"  },
            new { ServiceNumber = "D1-7445", NameEn = "Khalid Abdullah Al Harthi", Email = "khalid.harthi@example.com", UserName = "D1-7445"  },
            new { ServiceNumber = "D1-7447", NameEn = "Yousuf Nasser Al Riyami",   Email = "yousuf.riyami@example.com", UserName = "D1-7447" },
            new { ServiceNumber = "D1-7448", NameEn = "Mohammed Ali Al Kindi",     Email = "mohammed.kindi@example.com",UserName = "D1-7448"  }
        };

        foreach (var emp in employees)
        {
            var existingUser = await _userRepository.FindByNormalizedUserNameAsync(
                emp.UserName.ToUpperInvariant());

            if (existingUser != null) continue;

            var nameParts = emp.NameEn.Split(' ');
            var firstName = nameParts[0];
            var lastName = string.Join(" ", nameParts.Skip(1));

            var user = new IdentityUser(
                _guidGenerator.Create(),
                emp.UserName,
                emp.Email)
            {
                Name = firstName,
                Surname = lastName
            };

            // Set a default password — change in production
            (await _userManager.CreateAsync(user, "1q2w3E*")).CheckErrors();

            // Optional: set extra properties to link back to employee
            user.SetProperty("ServiceNumber", emp.ServiceNumber);
            await _userRepository.UpdateAsync(user);
        }
    }
}