namespace ShareForFuture.Data;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



public class OfferingSearch
{
    private readonly S4fDbContext context;

    public OfferingSearch(S4fDbContext context)
    {
        this.context = context;
    }

    public IQueryable<Offering> SingleWordSearchLinq(string searchString)
    {
        return context.Offerings
            .Where(offering => offering.Title.Contains(searchString))
            .Where(offering => offering.Description.Contains(searchString))
            .Where(offering => offering.Tags.Any(tag => tag.Tag.Contains(searchString)))
            .AsNoTracking();
    }
}

