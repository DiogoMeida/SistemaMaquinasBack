﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaMaquinas.Classes;
using SistemaMaquinas.Models;

namespace SistemaMaquinas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DefeitoExteriorController : ControllerBase
    {
        private readonly ILogger<DefeitoExteriorController> _logger;
        private readonly string _connectionString;


        public DefeitoExteriorController(ILogger<DefeitoExteriorController> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<DefeitoExterior>();
            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand("select eE.*, m.MODELO from DefeitoExterior eE left outer join Maquinas m on (eE.serial = m.serial)", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new DefeitoExterior
                            {
                                Serial = leitor["SERIAL"].ToString(),
                                Modelo = leitor["MODELO"].ToString(),
                                Local = leitor["LOCAL"].ToString()
                            });
                        }
                    }
                    return Ok(dados);
                }
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> MoverParaArmario2([FromBody] MoverParaArmario2 request)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    using (var comando = new SqlCommand($@"DECLARE @usuario int
                                                           SET @usuario = (SELECT idUsuario FROM users WHERE loginUsuario = '{request.usuario}') 
                                                           INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, USUARIO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, Motivo, CAIXA, DATA, CNPF, DataAlteracao)
                                                           SELECT SERIAL, 'DefeitoExterior', 'ARMARIO_2', @usuario, '', '', LOCAL, '', '', '', '', '', '', '', GETDATE() FROM DefeitoExterior
                                                           WHERE SERIAL = '{request.Serial}'
                                                           INSERT INTO ARMARIO_2(SERIAL, STATUS, SITUACAO, LOCAL)
                                                           SELECT SERIAL, 'LOGISTICA REVERSA', '{request.Situacao}', '{request.Local}' FROM DefeitoExterior WHERE SERIAL = '{request.Serial}'                                                            
                                                           DELETE FROM DefeitoExterior
                                                           WHERE SERIAL = '{request.Serial}';", conexao)
                                                        )
                    {
                        await comando.ExecuteNonQueryAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao mover o serial {request.Serial} para a tabela ARMARIO_2");
                return StatusCode(500);
            }
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Modelos()
        {
            var CidadesEstrangeiras = new List<CidadesEstrangeiras>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand(@"SELECT
                                                         SUM(CASE WHEN LOCAL = 4 THEN 1 ELSE 0 END) AS 'RIO PRETO',
                                                         SUM(CASE WHEN LOCAL= 6 THEN 1 ELSE 0 END) AS 'CAMPO GRANDE',
                                                         SUM(CASE WHEN LOCAL = 8 THEN 1 ELSE 0 END) AS 'RIO DE JANEIRO',
                                                         SUM(CASE WHEN LOCAL = 9 THEN 1 ELSE 0 END) AS 'CX PATRICK',
                                                         SUM(CASE WHEN LOCAL = 10 THEN 1 ELSE 0 END) AS 'DECIO',
                                                         SUM(CASE WHEN LOCAL = 7 THEN 1 ELSE 0 END) AS 'EVENTO 3A',
                                                         COUNT(SERIAL) AS Total
                                                       FROM DefeitoExterior", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            CidadesEstrangeiras.Add(new CidadesEstrangeiras
                            {
                                RioPreto = leitor["RIO PRETO"].ToString(),
                                CampoGrande = leitor["CAMPO GRANDE"].ToString(),
                                RioDeJaneiro = leitor["RIO DE JANEIRO"].ToString(),
                                CxPatrick = leitor["CX PATRICK"].ToString(),
                                Decio = leitor["DECIO"].ToString(),
                                Evento3A = leitor["EVENTO 3A"].ToString(),
                                Total = leitor["Total"].ToString()
                            });
                        }
                    }
                    return Ok(CidadesEstrangeiras);
                }
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ModelosReais()
        {
            var modelos = new List<Modelos>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand(@"SELECT
                                                         SUM(CASE WHEN m.MODELO = 'D3 - PRO 1' THEN 1 ELSE 0 END) AS 'D3 - PRO 1',
                                                         SUM(CASE WHEN m.MODELO = 'D3 - PRO 2' THEN 1 ELSE 0 END) AS 'D3 - PRO 2',
                                                         SUM(CASE WHEN m.MODELO = 'D3 - PRO REFURBISHED' THEN 1 ELSE 0 END) AS 'D3 - PRO REFURBISHED',
                                                         SUM(CASE WHEN m.MODELO = 'D3 - SMART' THEN 1 ELSE 0 END) AS 'D3 - SMART',
                                                         SUM(CASE WHEN m.MODELO = 'D3 - X' THEN 1 ELSE 0 END) AS 'D3 - X',
                                                         COUNT(a.SERIAL) AS Total
                                                       FROM
                                                         DefeitoExterior a
                                                         LEFT OUTER JOIN Maquinas m ON a.SERIAL = m.serial;", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            modelos.Add(new Modelos
                            {
                                d3Pro1 = leitor["D3 - PRO 1"].ToString(),
                                d3Pro2 = leitor["D3 - PRO 2"].ToString(),
                                d3ProRefurbished = leitor["D3 - PRO REFURBISHED"].ToString(),
                                d3Smart = leitor["D3 - SMART"].ToString(),
                                d3X = leitor["D3 - X"].ToString(),
                                Total = leitor["Total"].ToString()
                            });
                        }
                    }
                    return Ok(modelos);
                }
            }
        }

    }
}
