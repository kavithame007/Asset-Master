using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Asset_Master.Entities;

namespace Asset_Master.Helpers;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // CreateRequest -> User
        CreateMap<Createassets, assets>();

        // UpdateRequest -> User
        //CreateMap<CreateCandidate, Candidate>()
        //    .ForAllMembers(x => x.Condition(
        //        (src, dest, prop) =>
        //        {
        //            // ignore both null & empty string properties
        //            if (prop == null) return false;
        //            if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;

        //            return true;
        //        }
        //    ));
    }

}

