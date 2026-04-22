using System;
using DepartmentService.Application.DTOs;
using DepartmentService.Domain.Entities;
using Mapster;

namespace DepartmentService.Application.Mappings;

public class DepartmentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Department → DepartmentDto (flatten EmployeeCount from Stats)
        config.NewConfig<Department, DepartmentDto>()
            .Map(dest => dest.EmployeeCount,
                 src => src.Stats != null ? src.Stats.EmployeeCount : 0);

        // CreateDepartmentDto → Department
        config.NewConfig<CreateDepartmentDto, Department>()
            .Map(dest => dest.CreatedDate, _ => DateTime.UtcNow)
            .Ignore(dest => dest.DepartmentId)
            .Ignore(dest => dest.Stats!);

        // UpdateDepartmentDto → Department
        config.NewConfig<UpdateDepartmentDto, Department>()
            .Ignore(dest => dest.DepartmentId)
            .Ignore(dest => dest.CreatedDate)
            .Ignore(dest => dest.Stats!);
    }
}
