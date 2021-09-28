using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;



var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

List<Variable> variables = new List<Variable>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.MapGet("/api/variables", () => {    
    return JsonSerializer.Serialize(variables);
});

app.MapPost("/api/variables", (Variable v) => variables.Add(v));

app.MapPost("/api/evaluate", (Expression term) => {
    string expression = term.expression;
    if (checkRegex(term.expression))
    {
        variables.ForEach(v => { expression = expression.Replace(Char.Parse(v.name), Char.Parse(v.value.ToString())); });
        return Convert.ToDouble(new DataTable().Compute(expression, null));
    }
    return 400;
});

bool checkRegex(string equasion)
{
    Regex reg = new Regex(@"((\d+)|([a-zA-Z_]+))([+-]((\d+)|([a-zA-Z_]+)))*");
    return reg.IsMatch(equasion);
}

app.Run();

public record Variable(string name, int value);

public record Expression(string expression);