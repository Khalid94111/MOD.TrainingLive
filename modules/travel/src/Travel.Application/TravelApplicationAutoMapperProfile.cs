using System.Collections.Generic;
using System.Text.Json;
using AutoMapper;
using Travel.Allowances;
using Travel.TravelRequests;
using Travel.TravelTypes;

namespace Travel;

public class TravelApplicationAutoMapperProfile : Profile
{
    public TravelApplicationAutoMapperProfile()
    {
        CreateMap<TravelRequest, TravelRequestDto>();
        CreateMap<TravelRequestEmployee, TravelRequestEmployeeDto>();
        CreateMap<TravelDocument, TravelDocumentDto>();
        CreateMap<TravelFlightOffer, TravelFlightOfferDto>();
        CreateMap<TravelEmployeeDocument, TravelEmployeeDocumentDto>();
        CreateMap<AllowanceSnapshot, AllowanceSnapshotDto>();
        CreateMap<AllowanceRule, AllowanceRuleDto>();
        CreateMap<AllowanceRuleSegment, AllowanceRuleSegmentDto>();
        CreateMap<AllowanceRate, AllowanceRateDto>();
        CreateMap<ClothingAllowanceRule, ClothingAllowanceRuleDto>();
        CreateMap<TravelTypeDefinition, TravelTypeDefinitionDto>();
        CreateMap<AccommodationRule, AccommodationRuleDto>();
        CreateMap<TravelRequestAllowanceDetail, TravelRequestAllowanceDetailDto>()
            .ForMember(d => d.Segments, opt => opt.MapFrom(s => DeserializeSegments(s.SegmentsJson)));
    }

    private static List<AllowanceCalculationSegment> DeserializeSegments(string segmentsJson)
    {
        if (string.IsNullOrWhiteSpace(segmentsJson))
        {
            return new List<AllowanceCalculationSegment>();
        }

        return JsonSerializer.Deserialize<List<AllowanceCalculationSegment>>(segmentsJson) ?? new List<AllowanceCalculationSegment>();
    }
}
