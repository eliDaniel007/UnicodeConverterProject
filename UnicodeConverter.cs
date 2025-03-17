/*
* Projet de Conversion Unicode-32 <-> UTF-8
* 
*
* Description :
* Ce programme implémente la conversion bidirectionnelle entre Unicode-32 et UTF-8.
* Unicode-32 encode chaque caractère sur 4 octets en little-endian, tandis que UTF-8
* utilise un encodage variable de 1 à 4 octets selon la valeur du caractère.
*
* Le code gère :
* - La conversion d'un code point Unicode-32 vers sa séquence UTF-8 correspondante
* - La conversion d'une séquence UTF-8 vers son code point Unicode-32
* - La conversion de fichiers complets dans les deux sens
* - La validation des séquences et la gestion des erreurs
*/

using System;
using System.IO;

public class UnicodeConverter
{
    /// <summary>
    /// Convertit un code point Unicode-32 en séquence d'octets UTF-8.
    /// L'encodage UTF-8 utilise un nombre variable d'octets selon la plage de valeurs :
    /// - 0x0000-0x007F : 1 octet  (caractères ASCII)
    /// - 0x0080-0x07FF : 2 octets (caractères latins étendus, etc.)
    /// - 0x0800-0xFFFF : 3 octets (autres caractères BMP)
    /// - 0x10000-0x10FFFF : 4 octets (caractères supplémentaires)
    /// </summary>
    public static byte[] Unicode32ToUtf8(uint unicode32)
    {
        // Cas 1 octet : caractères ASCII (0-127)
        // Format : 0xxxxxxx
        if (unicode32 <= 0x7F)
        {
            return new byte[] { (byte)unicode32 };
        }
        // Cas 2 octets : caractères étendus (128-2047)
        // Format : 110xxxxx 10xxxxxx
        else if (unicode32 <= 0x7FF)
        {
            return new byte[] {
                (byte)(0xC0 | (unicode32 >> 6)),         // 110xxxxx : 5 bits de poids fort
                (byte)(0x80 | (unicode32 & 0x3F))        // 10xxxxxx : 6 bits de poids faible
            };
        }
        // Cas 3 octets : BMP (2048-65535)
        // Format : 1110xxxx 10xxxxxx 10xxxxxx
        else if (unicode32 <= 0xFFFF)
        {
            return new byte[] {
                (byte)(0xE0 | (unicode32 >> 12)),        // 1110xxxx : 4 bits de poids fort
                (byte)(0x80 | ((unicode32 >> 6) & 0x3F)), // 10xxxxxx : 6 bits du milieu
                (byte)(0x80 | (unicode32 & 0x3F))        // 10xxxxxx : 6 bits de poids faible
            };
        }
        // Cas 4 octets : caractères supplémentaires (65536-1114111)
        // Format : 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
        else if (unicode32 <= 0x10FFFF)
        {
            return new byte[] {
                (byte)(0xF0 | (unicode32 >> 18)),         // 11110xxx : 3 bits de poids fort
                (byte)(0x80 | ((unicode32 >> 12) & 0x3F)), // 10xxxxxx : 6 bits suivants
                (byte)(0x80 | ((unicode32 >> 6) & 0x3F)),  // 10xxxxxx : 6 bits suivants
                (byte)(0x80 | (unicode32 & 0x3F))         // 10xxxxxx : 6 bits de poids faible
            };
        }
        throw new ArgumentException("Code point invalide : dépasse 0x10FFFF");
    }

    /// <summary>
    /// Convertit une séquence d'octets UTF-8 en code point Unicode-32.
    /// Analyse le premier octet pour déterminer la longueur de la séquence,
    /// puis reconstruit le code point à partir des octets de continuation.
    /// </summary>
    public static uint Utf8ToUnicode32(byte[] utf8, ref int position)
    {
        byte firstByte = utf8[position];
        uint result;
        int bytesToRead;

        // Analyse du premier octet pour déterminer la longueur
        if ((firstByte & 0x80) == 0)          // 0xxxxxxx : séquence 1 octet
        {
            result = firstByte;
            bytesToRead = 1;
        }
        else if ((firstByte & 0xE0) == 0xC0)  // 110xxxxx : séquence 2 octets
        {
            result = (uint)(firstByte & 0x1F); // Garde les 5 bits de données
            bytesToRead = 2;
        }
        else if ((firstByte & 0xF0) == 0xE0)  // 1110xxxx : séquence 3 octets
        {
            result = (uint)(firstByte & 0x0F); // Garde les 4 bits de données
            bytesToRead = 3;
        }
        else if ((firstByte & 0xF8) == 0xF0)  // 11110xxx : séquence 4 octets
        {
            result = (uint)(firstByte & 0x07); // Garde les 3 bits de données
            bytesToRead = 4;
        }
        else
        {
            throw new ArgumentException("Premier octet UTF-8 invalide");
        }

        // Traitement des octets de continuation (format 10xxxxxx)
        for (int i = 1; i < bytesToRead; i++)
        {
            if (position + i >= utf8.Length)
                throw new ArgumentException("Séquence UTF-8 tronquée");

            byte nextByte = utf8[position + i];
            if ((nextByte & 0xC0) != 0x80)  // Vérifie le format 10xxxxxx
                throw new ArgumentException("Octet de continuation UTF-8 invalide");

            // Accumule les 6 bits de données de chaque octet
            result = (result << 6) | (uint)(nextByte & 0x3F);
        }

        position += bytesToRead - 1;  // Avance la position pour le prochain caractère
        return result;
    }

    /// <summary>
    /// Convertit un fichier entier entre les formats Unicode-32 et UTF-8.
    /// Gère le format little-endian pour Unicode-32 (4 octets par caractère).
    /// </summary>
    public static void ConvertFile(string inputFile, string outputFile, bool isU32ToUtf8)
    {
        try
        {
            if (isU32ToUtf8)
            {
                // Lecture du fichier Unicode-32 et conversion vers UTF-8
                using (var reader = new BinaryReader(File.OpenRead(inputFile)))
                using (var writer = new BinaryWriter(File.Create(outputFile)))
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        byte[] bytes = reader.ReadBytes(4);  // Lit un caractère U32 (4 octets)
                        uint unicode32 = BitConverter.ToUInt32(bytes, 0);  // Convertit en little-endian
                        byte[] utf8 = Unicode32ToUtf8(unicode32);
                        writer.Write(utf8);
                    }
                }
            }
            else
            {
                // Lecture du fichier UTF-8 et conversion vers Unicode-32
                byte[] utf8Data = File.ReadAllBytes(inputFile);
                using (var writer = new BinaryWriter(File.Create(outputFile)))
                {
                    int position = 0;
                    while (position < utf8Data.Length)
                    {
                        uint unicode32 = Utf8ToUnicode32(utf8Data, ref position);
                        writer.Write(BitConverter.GetBytes(unicode32));  // Écrit en little-endian
                        position++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur lors de la conversion : {ex.Message}");
        }
    }
}