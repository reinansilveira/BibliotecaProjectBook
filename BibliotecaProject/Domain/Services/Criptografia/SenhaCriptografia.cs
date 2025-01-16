using System.Security.Cryptography;
using System.Text;

namespace BibliotecaProject.Domain.Services.Criptografia;

public class SenhaCriptografia
{
    public string Criptografar(string senha)
    {
        var chaveAdicional = "ABC";
        
        var novaSenha = $"{senha}{chaveAdicional}";
        
        var bytes = Encoding.UTF8.GetBytes(novaSenha);
        var hashBytes = SHA512.HashData(bytes);
        
        return ConverterBytesString(hashBytes);
    }

    private static string ConverterBytesString(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            var hex = b.ToString("x2");
            sb.Append(hex);
        }
        return sb.ToString();
    }
}