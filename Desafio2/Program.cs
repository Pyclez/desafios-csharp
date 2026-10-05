using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Desafio2;

public class Produto{
    [JsonPropertyName("codigoProduto")]
    public int CodigoProduto { get; set; }

    [JsonPropertyName("descricaoProduto")]
    public string DescricaoProduto { get; set; } = "";

    [JsonPropertyName("estoque")]
    public int Estoque { get; set; }
}

public class ArquivoEstoque{
    [JsonPropertyName("estoque")]
    public List<Produto> Estoque { get; set; } = new();
}

public enum TipoMovimentacao{
    Entrada,
    Saida
}

public record Movimentacao(
    int Id,
    DateTime Data,
    int CodigoProduto,
    TipoMovimentacao Tipo,
    string Descricao,
    int Quantidade,
    int EstoqueFinal
);

//Criei uma exception especifica para esse programa

public class EstoqueException : Exception{
    public EstoqueException(string mensagem) : base(mensagem) { }
}

public class Deposito{
    private readonly Dictionary<int, Produto> _produtos;
    private readonly List<Movimentacao> _movimentacoes = new();
    private int _proximoId = 1;

    public Deposito(IEnumerable<Produto> produtos){
        _produtos = produtos.ToDictionary(p => p.CodigoProduto);
    }

    public IEnumerable<Produto> Produtos => _produtos.Values.OrderBy(p => p.CodigoProduto);

    public IReadOnlyList<Movimentacao> Movimentacoes => _movimentacoes;

    public Produto BuscarProduto(int codigoProduto){
        if(!_produtos.TryGetValue(codigoProduto, out var produto)){
            throw new EstoqueException($"Produto {codigoProduto} não encontrado.");
        }
        return produto;
    }

    public Movimentacao Movimentar(int codigoProduto, TipoMovimentacao tipo, int quantidade, string descricao){
        var produto = BuscarProduto(codigoProduto);

        if(quantidade <= 0)
            throw new EstoqueException("A quantidade deve ser maior que zero.");
        if(string.IsNullOrWhiteSpace(descricao))
            throw new EstoqueException("A descrição da movimentação é obrigatória.");
        if(tipo == TipoMovimentacao.Saida && quantidade > produto.Estoque)
            throw new EstoqueException(
                $"Estoque insuficiente: há {produto.Estoque} un. de {produto.DescricaoProduto}.");

        produto.Estoque += tipo == TipoMovimentacao.Entrada ? quantidade : -quantidade;

        var movimentacao = new Movimentacao(
            Id: _proximoId++,
            Data: DateTime.Now,
            CodigoProduto: codigoProduto,
            Tipo: tipo,
            Descricao: descricao.Trim(),
            Quantidade: quantidade,
            EstoqueFinal: produto.Estoque);
        
        _movimentacoes.Add(movimentacao);
        return movimentacao;
    }
}

public static class Program{
    public static int Main(){
        Console.OutputEncoding = Encoding.UTF8;

        string caminho = Path.Combine(AppContext.BaseDirectory, "estoque.json");

        if(!File.Exists(caminho)){
            Console.Error.WriteLine($"Arquivo não encontrado: {caminho}");
            return 1;
        }

        ArquivoEstoque? dados;
        try{
            dados = JsonSerializer.Deserialize<ArquivoEstoque>(File.ReadAllText(caminho));
        }catch(JsonException e){
            Console.Error.WriteLine($"JSON inválido: {e.Message}");
            return 1;
        }

        if(dados is null || dados.Estoque.Count == 0){
            Console.Error.WriteLine("Nenhum produto encontrado no arquivo.");
            return 1;
        }

        var deposito = new Deposito(dados.Estoque);

        while (true){
            Console.WriteLine();
            Console.WriteLine("=== Controle de estoque ===");
            Console.WriteLine("1 - Listar estoque");
            Console.WriteLine("2 - Lançar movimentação");
            Console.WriteLine("3 - Listar movimentações");
            Console.WriteLine("0 - Sair");

            string? opcao = Console.ReadLine()?.Trim();

            switch (opcao){
                case "1":
                    ListarEstoque(deposito);
                    break;
                case "2":
                    LancarMovimentacao(deposito);
                    break;
                case "3":
                    ListarMovimentacoes(deposito);
                    break;
                case "0":
                case null:
                    return 0;
                default:
                    Console.WriteLine("Opção inválida.");
                    break;
            }
        }
    }

    private static void ListarEstoque(Deposito deposito){
        Console.WriteLine();
        Console.WriteLine($"{"Código",-8}{"Produto", -28}{"Estoque",8}");
        Console.WriteLine(new string('-', 46));

        foreach (var p in deposito.Produtos){
            Console.WriteLine($"{p.CodigoProduto, -8}{p.DescricaoProduto,-28}{p.Estoque,8}");
        }
    }

    private static void LancarMovimentacao(Deposito deposito){
        ListarEstoque(deposito);
        Console.WriteLine();

        try{
            int codigo = LerInteiro("Código do produto: ");
            var produto = deposito.BuscarProduto(codigo);

            Console.Write("Tipo (E = entrada, S = saída): ");
            TipoMovimentacao tipo = Console.ReadLine()?.Trim().ToUpperInvariant() switch{
                "E" => TipoMovimentacao.Entrada,
                "S" => TipoMovimentacao.Saida,
                _ => throw new EstoqueException("Tipo inválido. Informe E ou S.")
            };

            int quantidade = LerInteiro("Quantidade: ");

            Console.Write("Descrição (ex.: compra de fornecedor, venda, devolução): ");
            string descricao = Console.ReadLine() ?? "";

            var mov = deposito.Movimentar(codigo, tipo, quantidade, descricao);

            Console.WriteLine();
            Console.WriteLine($"Movimentação nº {mov.Id} registrada: {NomeTipo(mov.Tipo)} de {mov.Quantidade} un. - {mov.Descricao}");
            Console.WriteLine($"Estoque final de {produto.DescricaoProduto}: {mov.EstoqueFinal}");
        }catch (EstoqueException e){
            Console.WriteLine($"Movimentação não realizada: {e.Message}");
        }
    }

    private static void ListarMovimentacoes(Deposito deposito){
        Console.WriteLine();

        if(deposito.Movimentacoes.Count == 0){
            Console.WriteLine("Nenhuma movimentação lançada.");
            return;
        }

        Console.WriteLine($"{"Id",-4}{"Data",-17}{"Cód.",-5}{"Tipo",-8}{"Qtde",6}{"Estoque",8} Descrição");
        Console.WriteLine(new string('-',75));

        foreach (var m in deposito.Movimentacoes){
            Console.WriteLine(
                $"{m.Id,-4}{m.Data,-17:dd/MM/yyyy HH:mm}{m.CodigoProduto,-5}" +
                $"{NomeTipo(m.Tipo),-8}{m.Quantidade,6}{m.EstoqueFinal,8} {m.Descricao}");
        } 

    }

    private static string NomeTipo(TipoMovimentacao tipo) =>
        tipo == TipoMovimentacao.Entrada ? "Entrada" : "Saída";

    private static int LerInteiro(string mensagem){
        Console.Write(mensagem);

        if (!int.TryParse(Console.ReadLine(), out int valor))
            throw new EstoqueException("Valor inválido: informe um número inteiro.");
        
        return valor;
    }
}