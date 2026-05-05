using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Helpers
{
    public static class UserHelper
    {
        public static async Task<bool> IsCategoryAdminAsync(this ITempDataDictionary tempData, string userId, ForumDbContext context)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            return await context.CategoryAdmins
                .AnyAsync(ca => ca.UserId == userId && ca.IsActive);
        }

        public static async Task<List<int>> GetAdminCategoriesAsync(string userId, ForumDbContext context)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<int>();

            return await context.CategoryAdmins
                .Where(ca => ca.UserId == userId && ca.IsActive)
                .Select(ca => ca.SubCategoryId)
                .ToListAsync();
        }
    }
}
