using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Extensions {
    public static class ListExtensions {
        /// <summary>
        /// Разбивает список на подсписки указанного размера.
        /// </summary>
        /// <typeparam name="T">Тип элементов списка.</typeparam>
        /// <param name="source">Исходный список.</param>
        /// <param name="chunkSize">Размер подсписка.</param>
        /// <returns>Список подсписков.</returns>
        public static IEnumerable<List<T>> Chunk<T>(this List<T> source, int chunkSize) {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Размер чанка должен быть больше 0.");

            for (int i = 0; i < source.Count; i += chunkSize) {
                yield return source.GetRange(i, Math.Min(chunkSize, source.Count - i));
            }
        }


        /// <summary>
        /// Разбивает коллекцию на подколлекции указанного размера.
        /// </summary>
        /// <typeparam name="T">Тип элементов коллекции.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="chunkSize">Размер чанка.</param>
        /// <returns>Список подколлекций.</returns>
        public static IEnumerable<ICollection<T>> Chunk<T>(this ICollection<T> source, int chunkSize) {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Размер чанка должен быть больше 0.");

            var chunk = new List<T>(chunkSize);
            foreach (var item in source) {
                chunk.Add(item);
                if (chunk.Count == chunkSize) {
                    yield return chunk;
                    chunk = new List<T>(chunkSize);
                }
            }

            if (chunk.Count > 0)
                yield return chunk;
        }
    }
}
