namespace AquaGas.Shared.OpenApi;

/// <summary>
/// Constantes padronizadas para tags, descrições e exemplos da documentação OpenAPI.
/// </summary>
public static class ApiDocumentation
{
    public const string Title = "AquaGas API";
    public const string Version = "v1";
    public const string Description =
        "API para gerenciamento de sistema de distribuição de água e gás. " +
                              "Oferece funcionalidades completas de gestão de clientes, funcionários, produtos, " +
                              "planos e vendas com suporte a autenticação JWT.";

    public static class Tags
    {
        public const string Auth = "Autenticação";
        public const string Customers = "Clientes";
        public const string Employees = "Funcionários";
        public const string Products = "Produtos";
        public const string Sales = "Vendas";
        public const string Plans = "Planos";
        public const string Penalties = "Penalidades";
        public const string Reports = "Relatórios";
    }

    public static class Security
    {
        public const string BearerSchemeId = "Bearer";
        public const string BearerDescription =
            "Autenticação JWT. Informe o token no formato: Bearer {seu_token}";
    }

    public static class Responses
    {
        public const string BadRequestDescription =
            "Requisição inválida ou falha de validação (código VALIDATION_ERROR).";
        public const string UnauthorizedDescription =
            "Não autenticado ou token inválido/expirado (código UNAUTHORIZED).";
        public const string ForbiddenDescription =
            "Autenticado, porém sem permissão para o recurso (código FORBIDDEN).";
        public const string NotFoundDescription =
            "Recurso não encontrado (código NOT_FOUND).";
        public const string ConflictDescription =
            "Conflito de regra de negócio (código CONFLICT ou INSUFFICIENT_STOCK).";
        public const string InternalErrorDescription =
            "Erro interno não tratado (código INTERNAL_ERROR).";
    }
}
