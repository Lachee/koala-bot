using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Database
{
    public interface IData
    {
        Task LoadAsync(DbContext db);
        Task SaveAsync(DbContext db);
    }
}
