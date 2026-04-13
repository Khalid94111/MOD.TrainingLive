using MOD.Training.Training.Centers;
using MOD.Training.Training.Centers.Dtos;
using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateCourseSessionToEntityMapper : MapperBase<CreateUpdateCourseSessionDto, CourseSession>
{
    public override   CourseSession Map(CreateUpdateCourseSessionDto source)
    {
        var entity = new CourseSession(Guid.NewGuid(), source.CourseId, source.SessionCode, source.StartDate, source.EndDate, source.MaxSeats);
        Map(source, entity);
        return entity;
    }
    public override partial void Map(CreateUpdateCourseSessionDto source, CourseSession destination);
}

 