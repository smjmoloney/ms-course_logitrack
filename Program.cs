using ms_course_logitrack.Data;
using ms_course_logitrack.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<LogiTrackContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var context = new LogiTrackContext())
{
    // Add test inventory item if none exist
    if (!context.InventoryItems.Any())
    {
        context.InventoryItems.Add(new InventoryItem
        {
            Name = "Pallet Jack",
            Quantity = 12,
            Location = "Warehouse A"
        });

        context.SaveChanges();
    }

    // Retrieve and print inventory to confirm
    var items = context.InventoryItems.ToList();
    foreach (var item in items)
    {
        Console.WriteLine(item.DisplayInfo()); // Should print: Item: Pallet Jack | Quantity: 12 | Location: Warehouse A
    }
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();