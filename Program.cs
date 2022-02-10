using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.IO.Compression;

namespace GPS_Pathfinder
{
    class Program
    {
        static void Main(string[] args)
        {
            MainLoad(); //função principal, é ela que faz todo o trabalho
            Write.Info("Pressione qualquer tecla para sair.");
            Console.ReadLine();
        }

        static void MainLoad()
        {
            //se for "sim", o programa baixa apenas um dia da base, se for "não", podem ser baixados vários dias.
            Write.Info("Deseja baixar dados de um dia específico [S/N] ? ", false);
            char c = Console.ReadKey().KeyChar;

            //atualizo a variável "BaixarUmDia", que é falsa por padrão
            if (c == 'S' || c == 's') { Config.BaixarUmDia = true; }

            //se a variável "BaixarUmDia" é verdadeira, baixamos apenas um dia da base.
            if (Config.BaixarUmDia)
            {
                Write.Info("\nInforme a base que deseja baixar os arquivos (ex. prgu): ",false);
                ibge.cod_base = Console.ReadLine();
                Write.Info("\nBaixar dia (ex. 01-01-2022): ",false);
                ibge.Download(CreateDate(Console.ReadLine())); //baxiamos o dia solicitado
                return; //FIM.
            }

            //só chegamos nessa parte do código se "BaixarUmDia" for falso, ou seja, daqui pra frente vamos baixar vários dias.
            Write.Info("\nDeseja baixar quantos dias anteriores? ", false);
            int dias_ant = int.Parse(Console.ReadLine());

            //os dias são criados por meio da função CreateDate, que conta os dias anteriores ao dia atual e retona um array de datas.
            foreach (DateTime d in CreateDate(dias_ant))
            {
                if (!ibge.Download(d))
                {
                    Write.Error("Erro!, não foi possível baixar a base do IBGE, de pelo menos um dia.", true);
                }
            }
        }

        // Cria datas anteriores à atual, com número dependente do int colocado na função (30 por padrão)
        // exemplo da função:
        //  hoje: 12/01/2022, Dias_Anteriores = 4
        //  retorno da função: [ 11/01/2022, 10/01/2022, 09/01/2022, 08/01/2022 ]
        static DateTime[] CreateDate(int Dias_Anteriores = 30)
        {
            DateTime[] lst = new DateTime[Dias_Anteriores];
            for (int i = 0; i < Dias_Anteriores; i++)
            {
                lst[i] = DateTime.Today.AddDays(-(i + 1));
            }
            return lst;
        }

        //Variação da função anterior, porém recebe uma string e retorna uma data (ex: string "01-01-2022" retorna a data 01-01-2022)
        static DateTime CreateDate(string str)
        {
            int dia = int.Parse(str.Split('-')[0]);
            int mes = int.Parse(str.Split('-')[1]);
            int ano = int.Parse(str.Split('-')[2]);
            return new DateTime(ano, mes, dia);
        }
    }

    //base do IBGE.
    public class ibge
    {

        public static string cod_base = "prgu"; // deixei como "base padrão" de Guarapuava (pode ser alterado pelo usuário)

        //checa a existência de um arquivo de base (para evitar baixar um arquivo que já existe no computador)
        public static bool Check(string file, DateTime data)
        {
            string a = file.Split('.')[0];
            string year2 = data.Year.ToString().Substring(2); //últimos 2 digitos do ano
            if (System.IO.File.Exists(Config.Dir + a + "." + year2 + "d"))
            {
                Write.Alert("Arquivo " + a + "." + year2 + "d" + " já existe.");
            }
            if (System.IO.File.Exists(Config.Dir + a + "." + year2 + "g"))
            {
                Write.Alert("Arquivo " + a + "." + year2 + "g" + " já existe.");
            }
            if (System.IO.File.Exists(Config.Dir + a + "." + year2 + "n"))
            {
                Write.Alert("Arquivo " + a + "." + year2 + "n" + " já existe.");
            }
            if (System.IO.File.Exists(Config.Dir + a + "." + year2 + "o"))
            {
                Write.Alert("Arquivo " + a + "." + year2 + "o" + " já existe.");
            }
            if (System.IO.File.Exists(Config.Dir + a + "." + year2 + "d") ||
                System.IO.File.Exists(Config.Dir + a + "." + year2 + "g") ||
                System.IO.File.Exists(Config.Dir + a + "." + year2 + "n") ||
                System.IO.File.Exists(Config.Dir + a + "." + year2 + "o")) { return true; }
            return false;
        }

        // Baixa o arquivo do IBGE de uma certa data (retorna o sucesso no download por meio de um booleano, true = concluído com sucesso; false = deu problema).
        public static bool Download(DateTime data)
        {
            try
            {
                // monta a URL do arquivo a ser baixado.
                string str1 = "geoftp.ibge.gov.br";
                string str2 = "/informacoes_sobre_posicionamento_geodesico/rbmc/dados/" + (object)data.Year + "/" + data.DayOfYear.ToString().PadLeft(3, '0') + "/";
                string str3 = cod_base + data.DayOfYear.ToString().PadLeft(3, '0') + "1.zip";
                string requestUriString = "ftp://" + str1 + str2 + str3;
                
                //Checa o arquivo, se já existe não precisamos baixá-lo novamente
                if (Check(str3, data)) { return true; }

                Write.Info("Baixando Arquivo " + str3 + " do dia " + (object)data + "...");
                Write.Info("Procurando Tamanho do Arquivo...", false);

                // Baixamos o arquivo, por meio de um Request FTP.
                FtpWebRequest ftpWebRequest1 = (FtpWebRequest)WebRequest.Create(requestUriString);
                ftpWebRequest1.Method = "SIZE";
                FtpWebResponse response = (FtpWebResponse)ftpWebRequest1.GetResponse();
                response.GetResponseStream();
                long TotalLen = response.ContentLength;
                Write.Success(TotalLen.ToString());
                response.Close();
                FtpWebRequest ftpWebRequest2 = (FtpWebRequest)WebRequest.Create(requestUriString);
                ftpWebRequest2.Method = "RETR";
                using (Stream responseStream = ftpWebRequest2.GetResponse().GetResponseStream())
                {
                    using (Stream stream = (Stream)System.IO.File.Create(Config.Dir + str3))
                    {
                        byte[] buffer = new byte[10240];
                        long total = 0;
                        int count;
                        Console.WriteLine("");
                        while ((count = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ClearLastLine();
                            Write.Info("Baixando... " + GetPct(total, TotalLen) + "%.");
                            stream.Write(buffer, 0, count);
                            total += count;
                        }
                        ClearLastLine();
                    }
                }
                Write.Success("OK.");

                // Extraímos o arquivo baixado.
                Write.Info("Extraindo Arquivo " + str3 + "...", false);
                if (Extract(str3))
                {
                    Write.Success("Completo.");
                    return true;
                }
                else { Write.Error("Erro."); return false; }
            }
            catch(Exception ex)
            {
                Write.Error("Erro. " + ex.Message);
                return false;
            }
        }

        //Função que serve pra auxiliar o processo de extração dos arquivos baixados.
        public static bool Extract(string file)
        {
            try
            {
                Write.Info("Arquivo => " + file);
                ZipFile.ExtractToDirectory(Config.Dir + file, Config.Dir);
                File.Delete(Config.Dir + file);
                return true;
            }
            catch(Exception ex) { Write.Error("Erro: " + ex.Message); return false; }
        }

        // "Limpa" a última linha do console, útil pra mostrar a porcentagem de download.
        public static void ClearLastLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        //  calcula a porcentagem do download.
        public static string GetPct(long n1, long n2)
        {
            if (n1 == 0) { n1 = 1; }
            if (n2 == 0) { n2 = 1; }
            double d = n1 * 100 / n2;
            return d.ToString();
        }
    }

    //Configurações do programa.
    public class Config
    {
        public static bool BaixarUmDia = false; // falsa por padrão, é alterada para true caso o usuário selecione "sim" no prompt de comando
        //  diretório padrão para salvar os arquivos do IBGE, eu sempre deixo em C:/base/, caso queira mudar, altere a variável abaixo.
        public static string Dir = "C:/base/";

    }

    // Facilita o processo de lançar no console avisos, erros, alertas, etc.
    public class Write
    {
        public static void Error(string text,bool NewLine = true)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[-] ");
            Console.ForegroundColor = ConsoleColor.White;
            if (NewLine) { Console.WriteLine(text); }
            else { Console.Write(text); }
        }
        public static void Info(string text, bool NewLine = true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[+] ");
            Console.ForegroundColor = ConsoleColor.White;
            if (NewLine) { Console.WriteLine(text); }
            else { Console.Write(text); }
        }
        public static void Alert(string text, bool NewLine = true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[*] ");
            Console.ForegroundColor = ConsoleColor.White;
            if (NewLine) { Console.WriteLine(text); }
            else { Console.Write(text); }
        }
        public static void Success(string text, bool NewLine = true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[+] ");
            Console.ForegroundColor = ConsoleColor.White;
            if (NewLine) { Console.WriteLine(text); }
            else { Console.Write(text); }
        }
    }
}
