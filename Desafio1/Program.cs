using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Desafio1;

public record Venda(
    [property: JsonPropertyName("vendedor")] string vendedor,
    [property: JsonPropertyName("valor")] decimal valor
);

public record ArquivoVendas(
    [property: JsonPropertyName("vendas")] List<Venda> vendas
);

public record ResumoVendedor(
    string Vendedor,
    int QuantidadeVendas,
    decimal TotalVendido,
    decimal Comissao
);


public static class CalculadoraComissao{
//Valores de referência
    private const decimal PercentualMenor = 0.01m;
    private const decimal PercentualMaior = 0.05m;
    private const decimal LimiteSemComissao = 100m;
    private const decimal LimiteComissaoMenor = 500m;


    public static decimal Calcular(decimal valorVenda) =>valorVenda switch {
        < LimiteSemComissao => 0m,
        < LimiteComissaoMenor => valorVenda * PercentualMenor,
        _ => valorVenda * PercentualMaior
    };

    public static List<ResumoVendedor> CalcularPorVendedor(IEnumerable<Venda> vendas) => 
    vendas
        .GroupBy(v => v.vendedor)
        .Select(r => new ResumoVendedor(
            Vendedor: r.Key,
            QuantidadeVendas: r.Count(),
            TotalVendido: r.Sum(v=>v.valor),
            Comissao: Math.Round(r.Sum(v => Calcular(v.valor)), 2, MidpointRounding.AwayFromZero)))
        .OrderByDescending(o => o.Comissao)
        .ToList();

}

public static class Program{

    //Nome do arquivo JSON
    private const string  nomeArquivo = "vendas.json";
    public static int Main(string[] args){
        Console.OutputEncoding = Encoding.UTF8;
        
        string Caminho = Path.Combine(AppContext.BaseDirectory, nomeArquivo);

        if (!File.Exists(Caminho)){
            Console.Error.WriteLine($"Arquivo não encontrado: {Caminho}");
            return 1;
        }

        ArquivoVendas? Dados;
        try{
            Dados = JsonSerializer.Deserialize<ArquivoVendas>(File.ReadAllText(Caminho));
        }catch(JsonException ex){
            Console.Error.WriteLine($"JSON inválido: {ex.Message}");
            return 1;
        }

        if(Dados?.vendas is null || Dados.vendas.Count == 0){
            Console.Error.WriteLine("Nenhuma venda encontrada no arquivo");
            return 1;
        }

        var resumos = CalculadoraComissao.CalcularPorVendedor(Dados.vendas);
        var ptBr = CultureInfo.GetCultureInfo("pt-BR");

        Console.WriteLine($"{"Vendedor",-18}{"Vendas",6}{"Total vendido",16}{"Comissão",14}");
        Console.WriteLine(new string('-', 57));

        foreach (var r in resumos){
            Console.WriteLine(
                $"{r.Vendedor,-18}{r.QuantidadeVendas,6}" +
                $"{r.TotalVendido.ToString("C2", ptBr),16}{r.Comissao.ToString("C2", ptBr),14}"
            );
        }

        Console.WriteLine(new string('-', 57));
        Console.WriteLine(
            $"{"Total",-18} {resumos.Sum(r => r.QuantidadeVendas),6}{resumos.Sum(r => r.TotalVendido).ToString("C2", ptBr),16}{resumos.Sum(r => r.Comissao).ToString("C2", ptBr),14}"
        );
        return 0;
    }
}