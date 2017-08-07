// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   Реализует потокобезопасную очередь
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FilesScan
{
    using System.Collections.Generic;

    /// <summary>
    /// Реализует потокобезопасную очередь
    /// </summary>
    /// <typeparam name="T">
    /// Тип элементов очереди
    /// </typeparam>
    public class SyncQueue<T>
    {
        /// <summary>
        /// элементы очереди
        /// </summary>
        private readonly Queue<T> items = new Queue<T>();

        /// <summary>
        /// Объект синхронизации
        /// </summary>
        private readonly object sync = new object();

        /// <summary>
        /// Извлекает элемент из очереди
        /// </summary>
        /// <returns>
        /// Тип элементов очереди <see cref="T"/>.
        /// </returns>
        public T Dequeue()
        {
            lock (this.sync)
            {
                return this.items.Dequeue();
            }
        }

        /// <summary>
        /// Добавляет элемент в очередь
        /// </summary>
        /// <param name="item">
        /// Добавляемый элемент
        /// </param>
        public void Enqueue(T item)
        {
            lock (this.sync)
            {
                this.items.Enqueue(item);
            }
        }

        /// <summary>
        /// Возвращает количество элементов в очереди
        /// </summary>
        public int Count
        {
            get
            {
                return this.items.Count;
            }
        }
    }
}
