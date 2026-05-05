namespace EsquireVRN.Models.DTO
{
    public class UpdateCategoryModel
    {
        public List<VRNSubCategoryDTO>? AddIdList { get; set; }
        public List<long>? RemoveIdList { get;set; }
    }
}
