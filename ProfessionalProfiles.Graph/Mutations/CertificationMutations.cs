using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using HotChocolate.Authorization;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Graph.Certfications;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Validations;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class CertificationMutations
    {
        /// <summary>
        /// Add User Certification
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddCertificationsAsync(List<CertificationInput> inputs,
            IRepositoryManager repository)
        {
            foreach (var input in inputs)
            {
                var validationResult = new CertificationInputValidator().Validate(input);
                if (!validationResult.IsValid)
                {
                    var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                    return new Payload(message);
                }
            }

            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
            }

            var certifications = inputs.Initialize(userId);
            await repository.Certification.AddRangeAsync(certifications);
            return new Payload("Certification added successfully", true);
        }

        /// <summary>
        /// Updated certification
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UpdateCertificationAsync(Guid id, CertificationInput input,
            IRepositoryManager repository)
        {
            var validationResult = new CertificationInputValidator().Validate(input);
            if (!validationResult.IsValid)
            {
                var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new Payload(message);
            }

            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
            }

            var certification = await repository.Certification.FindAsync(c => c.Id.Equals(id));
            if (certification.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(certification!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            certification = input.Map(certification!);
            await repository.Certification.EditAsync(c => c.Id.Equals(id), certification);
            return new Payload("Certification updated successfully", true);
        }

        /// <summary>
        /// Deletes certification records
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> DeleteCertificationAsync(Guid id, IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
            }

            var certification = await repository.Certification.FindAsync(c => c.Id.Equals(id));
            if (certification.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(certification!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            await repository.Certification.DeleteAsync(c => c.Id.Equals(id));
            return new Payload("Certification deleted successfully", true);
        }
    }
}
