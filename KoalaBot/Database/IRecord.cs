using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Database
{
    public interface IRecord
    {
        Task<bool> LoadAsync(DbContext db);
        Task<bool> SaveAsync(DbContext db);
    }
}
