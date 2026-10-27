namespace VeiculosApi.Application.Veiculos;

public sealed class VeiculoConflitoConcorrenciaException()
    : Exception("O veículo foi alterado por outra operação. Recarregue e tente novamente.");
