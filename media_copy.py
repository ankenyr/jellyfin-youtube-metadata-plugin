import os
import shutil
import glob
import pathlib
import signal

# Parameters to configure
src_directory = ''
dst_directory = ''
# ffmpeg -y -f lavfi -i testsrc=size=1920x1080:rate=1 -vf hue=s=0 -vcodec libx264 -preset superfast -tune zerolatency -pix_fmt yuv420p -t 10 -movflags +faststart vid.mp4
fake_vid = 'vid.mkv'
file_extension = '.info.json'  # Change to None if you want to move all files
file_name = ''
def signal_handler(sig, frame):
    print(file_name)


copy_extensions = ('.png', '.jpg', '.json', '.webp')
movie_extensions = ('.avi', '.mp4', '.mkv', '.m4a', '.mov', '.flv')
signal.signal(signal.SIGINFO, signal_handler)

# Execute the function
for i in glob.iglob(src_directory+'/**',recursive=True):
    fn = pathlib.Path(i)
    try:
        if fn.is_dir():
            continue
        rel_src_path = fn.relative_to(src_directory)
        dst_path = pathlib.Path(dst_directory + '/' + str(rel_src_path))
        if fn.suffix in copy_extensions:
            os.makedirs(dst_path.parent, exist_ok=True)
            shutil.copy(str(fn), str(dst_path))
        elif fn.suffix in movie_extensions:
            os.makedirs(dst_path.parent, exist_ok=True)
            shutil.copy(fake_vid, str(dst_path))
            
    except OSError:
        continue
    
    file_name = i
    