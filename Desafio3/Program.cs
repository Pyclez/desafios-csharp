using System.Globalization;
using System.Text;

namespace CalculoJuros;

public static class CalculadoraJuros{

    private const decimal TaxaDiaria = 0.025m;

    public static decimal Calcular(decimal valor, DateTime vencimento, DateTime hoje){
        int diasAtraso = (hoje.Date - vencimento.Date).Days;

        if(diasAtraso <= 0){
            return 0m;
        }
        return valor * TaxaDiaria * diasAtraso;
    }
}

public static class Program{
    public static void Main(){
        Console.OutputEncoding = Encoding.UTF8;
        var ptBr = CultureInfo.GetCultureInfo("pt-Br");

        Console.Write("Digite o valor:");

        if(!decimal.TryParse(Console.ReadLine()?.Trim(), NumberStyles.AllowDecimalPoint, ptBr, out decimal valor)){
           Console.WriteLine("Valor inválido!");
           return;
        }

        Console.Write("Digite a data de vencimento (dd/mm/aaaa): ");
        if(!DateTime.TryParseExact(Console.ReadLine()?.Trim(), "dd/MM/yyyy", ptBr, DateTimeStyles.None, out DateTime vencimento)){
            Console.WriteLine("Data inválida.");
            return;
        }

        decimal juros = CalculadoraJuros.Calcular(valor, vencimento, DateTime.Today);

        Console.WriteLine($"Valor dos juros em {DateTime.Today:dd/MM/yyyy}: {juros.ToString("C2", ptBr)}");
    }
}