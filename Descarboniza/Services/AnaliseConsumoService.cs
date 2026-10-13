using Descarboniza.Areas.Identity.Data;

namespace Descarboniza.Services
{
    public class AnaliseConsumoService
    {
        // Calcula o consumo teórico mensal com base na potência da máquina.
        public double CalcularConsumoTeorico(Consumo consumo)
        {
            if (consumo.Maquina == null)
                return 0;

            return consumo.Maquina.Potencia *
                   consumo.HorasUsoDia *
                   consumo.DiasUsoMes;
        }

        // Calcula o custo mensal de energia.
        public double CalcularCustoMensal(Consumo consumo)
        {
            return consumo.ConsumoKwhMes *
                   consumo.TarifaKwh;
        }

        // Calcula o custo anual de energia.
        public double CalcularCustoAnual(Consumo consumo)
        {
            return CalcularCustoMensal(consumo) * 12;
        }

        // Estima o consumo anual.
        public double CalcularConsumoAnual(Consumo consumo)
        {
            return consumo.ConsumoKwhMes * 12;
        }

        // Calcula o consumo médio por dia.
        public double CalcularConsumoDiario(Consumo consumo)
        {
            if (consumo.DiasUsoMes <= 0)
                return 0;

            return consumo.ConsumoKwhMes /
                   consumo.DiasUsoMes;
        }

        // Calcula o consumo médio por hora de funcionamento.
        public double CalcularConsumoPorHora(Consumo consumo)
        {
            double horasTotais =
                consumo.HorasUsoDia *
                consumo.DiasUsoMes;

            if (horasTotais <= 0)
                return 0;

            return consumo.ConsumoKwhMes /
                   horasTotais;
        }

        // Calcula o custo médio por dia.
        public double CalcularCustoDiario(Consumo consumo)
        {
            if (consumo.DiasUsoMes <= 0)
                return 0;

            return CalcularCustoMensal(consumo) /
                   consumo.DiasUsoMes;
        }

        // Calcula o custo médio por hora de funcionamento.
        public double CalcularCustoPorHora(Consumo consumo)
        {
            double horasTotais =
                consumo.HorasUsoDia *
                consumo.DiasUsoMes;

            if (horasTotais <= 0)
                return 0;

            return CalcularCustoMensal(consumo) /
                   horasTotais;
        }

        // Calcula a diferença entre o consumo teórico e o informado.
        public double CalcularDiferencaConsumo(Consumo consumo)
        {
            double consumoTeorico =
                CalcularConsumoTeorico(consumo);

            return consumoTeorico -
                   consumo.ConsumoKwhMes;
        }

        // Calcula a diferença percentual entre o consumo teórico e o informado.
        public double CalcularPercentualDiferenca(Consumo consumo)
        {
            double consumoTeorico =
                CalcularConsumoTeorico(consumo);

            if (consumoTeorico <= 0)
                return 0;

            double diferenca =
                CalcularDiferencaConsumo(consumo);

            return (diferenca / consumoTeorico) * 100;
        }

        // Faz uma classificação inicial do consumo.
        public string ClassificarConsumo(Consumo consumo)
        {
            double consumoTeorico =
                CalcularConsumoTeorico(consumo);

            if (consumoTeorico <= 0)
                return "Não foi possível realizar a classificação.";

            double percentual =
                CalcularPercentualDiferenca(consumo);

            if (percentual > 10)
            {
                return "O consumo informado está abaixo do consumo teórico.";
            }

            if (percentual < -10)
            {
                return "O consumo informado está acima do consumo teórico.";
            }

            return "O consumo informado está próximo do consumo teórico.";
        }
    }
}