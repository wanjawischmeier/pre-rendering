from compression import *
from random import randint
from sys import getsizeof as size
from timeit import default_timer as time
import matplotlib.pyplot as plt
from matplotlib import style
import tkinter as tk
from tkinter.ttk import Progressbar


def start_plotting(event = None):
    global l1
    global l2
    global e_from
    global e_to
    global btn
    global loading

    start = int(e_from.get())
    end = int(e_to.get())

    l1.destroy()
    l2.destroy()
    e_from.destroy()
    e_to.destroy()
    btn.destroy()

    loading.unbind_all('<Return>')

    width = 400
    height = 20

    x = (loading.winfo_screenwidth() // 2) - (width // 2)
    y = (loading.winfo_screenheight() // 2) - (height // 2) -50

    loading.geometry(f'{width}x{height}+{x}+{y}')

    bar = Progressbar(
        loading, 
        mode = 'determinate'
    )
    bar.grid(sticky = 'nsew')

    debug_mode = False
    data = {}

    for length in range(start, end +1):
        bar['value'] = (length - start) / (end - start) *100
        bar.update()

        l2 = [randint(0, 255) for i in range(length)]

        start_time = time()

        out_ind = compress('mni', l2)
        comp_time = time() - start_time

        out_l = decompress('mni', out_ind, length)
        decomp_time = time() - (start_time + comp_time)
        '''
        print(l2)
        print(f'{size(l2)} bytes -> {size(out_ind)} bytes')
        print(out_l['result'])
        print()
        '''
        compression_rate = ((size(l2) - size(out_ind)) *100) / size(l2)

        data[length] = {
            'compression time':     comp_time, 
            'decompression time':   decomp_time, 
            # 'total time':           total_time, 
            'compression_rate':     compression_rate
        }

    loading.destroy()

    plot_data(data)


def plot_data(data):
    style.use('ggplot')
    fig = plt.figure()

    window = plt.get_current_fig_manager().window
    window.title('Relationship of the input list size and the compression rate')
    window.attributes("-fullscreen", True)

    window.bind('<Escape>', reload)

    ax1 = fig.add_subplot(211)
    ax2 = fig.add_subplot(212)

    keys = list(data.keys())

    rate = [(data[item]['compression_rate']) for item in data]
    rate[0] = 0
    # time = [(data[item]['total time']) for item in data]
    comp_time = [(data[item]['compression time'] *1000) for item in data]
    decomp_time = [(data[item]['decompression time'] *1000) for item in data]

    # time = [(item / time[len(time) -1] *80) for item in time]
    # comp_time = [(item / comp_time[len(comp_time) -1] *80) for item in comp_time]
    # decomp_time = [(item / decomp_time[len(decomp_time) -1] *80) for item in decomp_time]

    # print(X)
    # print(Y)

    plt.title('Relationship of the input list size and the different compression aspects')
    plt.xlabel('Size of the input list')
    # plt.ylabel('Compression rate')
    # plt.xlim(1, total_length)
    # plt.ylim(0, 100)

    ax1.plot(keys, rate, label = 'Compression rate (in %)')
    ax2.plot(keys, comp_time, label = 'Compression time (in ms)')
    ax2.plot(keys, decomp_time, label = 'Decompression time (in ms)')
    # plt.plot(keys, time, label = 'Total time')
    ax1.legend()
    ax2.legend()
    # plt.scatter(keys, rate)

    plt.show()

def reload(event): plt.close(); main()


def main():
    global l1
    global l2
    global e_from
    global e_to
    global btn
    global loading

    loading = tk.Tk()
    loading.title('Compressing and decompressing data...')
    loading.bind('<Return>', start_plotting)

    width = 200
    height = 80

    x = (loading.winfo_screenwidth() // 2) - (width // 2)
    y = (loading.winfo_screenheight() // 2) - (height // 2) -50

    loading.geometry(f'{width}x{height}+{x}+{y}')
    loading.columnconfigure(0, weight=1)
    loading.rowconfigure(0, weight=1)
    loading.focus()

    l1 = tk.Label(loading, text = 'From:')
    l2 = tk.Label(loading, text = 'To:')
    l1.grid(row = 0)
    l2.grid(row = 1)

    e_from = tk.Entry(loading)
    e_from.grid(column = 1, row = 0, sticky = 'ew')
    e_from.focus()

    e_to = tk.Entry(loading)
    e_to.grid(column = 1, row = 1, sticky = 'ew')

    btn = tk.Button(loading, text = 'Start plotting', command = start_plotting)
    btn.grid(row = 2, columnspan = 2, sticky = 'ew')


if __name__ == "__main__":
    main()

tk.mainloop()