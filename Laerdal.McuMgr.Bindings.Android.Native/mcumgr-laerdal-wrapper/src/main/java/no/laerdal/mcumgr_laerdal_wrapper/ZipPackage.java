package no.laerdal.mcumgr_laerdal_wrapper;

import android.util.Pair;
import androidx.annotation.Keep;
import androidx.annotation.NonNull;
import com.google.gson.FieldNamingPolicy;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import java.io.*;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;

public final class ZipPackage {
    private static final String MANIFEST = "manifest.json";

    @SuppressWarnings({"unused", "MismatchedReadAndWriteOfArray"})
    @Keep
    private static class Manifest {
        private int formatVersion;
        private File[] files;

        @Keep
        private static class File {
            private String version;
            private String file;
            private int size;
            private int imageIndex;
        }
    }

    private Manifest manifest;
    private final List<Pair<Integer, byte[]>> binaries;

    public ZipPackage(@NonNull final byte[] data) throws IOException {
        final ZipInputStream zis = new ZipInputStream( // Unzip the file and look for the manifest.json
                new ByteArrayInputStream(data)
        );

        ZipEntry ze;
        Map<String, byte[]> entries = new HashMap<>();
        while ((ze = zis.getNextEntry()) != null) {

            if (ze.isDirectory())
                throw new IOException("Invalid ZIP");

            final String name = validateFilename(ze.getName(), ".");

            if (name.equals(MANIFEST)) {
                final Gson gson = new GsonBuilder()
                        .setFieldNamingPolicy(FieldNamingPolicy.LOWER_CASE_WITH_UNDERSCORES)
                        .create();

                manifest = gson.fromJson(new InputStreamReader(zis), Manifest.class);

            } else if (name.endsWith(".bin")) {
                final byte[] content = getData(zis);
                entries.put(name, content);
            }

        }

        binaries = new ArrayList<>(2);
        if (manifest == null) {
            // throw new IOException("Zip package doesn't contain a manifest file named '" + MANIFEST + "'!"); //nah   lets be a bit practical

            int i = 0;
            for (Map.Entry<String, byte[]> entry : entries.entrySet()) {
                binaries.add(new Pair<>(i++, entry.getValue()));
            }

        } else {
            for (final Manifest.File fileSpecs : manifest.files) { // Search for images
                final byte[] rawBytes = entries.get(fileSpecs.file);
                if (rawBytes == null)
                    throw new IOException("File not found: " + fileSpecs.file);

                binaries.add(new Pair<>(fileSpecs.imageIndex, rawBytes));
            }
        }

    }

    public List<Pair<Integer, byte[]>> getBinaries() {
        return binaries;
    }

    private byte[] getData(@NonNull ZipInputStream zis) throws IOException {
        final byte[] buffer = new byte[1024];

        // Read file content to byte array
        final ByteArrayOutputStream os = new ByteArrayOutputStream();
        int count;
        while ((count = zis.read(buffer)) != -1) {
            os.write(buffer, 0, count);
        }
        return os.toByteArray();
    }

    /**
     * Validates the path (not the content) of the zip file to prevent path traversal issues.
     *
     * <p> When unzipping an archive, always validate the compressed files' paths and reject any path
     * that has a path traversal (such as ../..). Simply looking for .. characters in the compressed
     * file's path may not be enough to prevent path traversal issues. The code validates the name of
     * the entry before extracting the entry. If the name is invalid, the entire extraction is aborted.
     * <p>
     *
     * @param filename    The path to the file.
     * @param intendedDir The intended directory where the zip should be.
     * @return The validated path to the file.
     * @throws java.io.IOException Thrown in case of path traversal issues.
     */
    @SuppressWarnings("SameParameterValue")
    private String validateFilename(@NonNull final String filename, @NonNull final String intendedDir) throws IOException {
        File f = new File(filename);
        String canonicalPath = f.getCanonicalPath();

        File iD = new File(intendedDir);
        String canonicalID = iD.getCanonicalPath();

        if (canonicalPath.startsWith(canonicalID)) {
            return canonicalPath.substring(1); // remove leading "/"
        } else {
            throw new IllegalStateException("File is outside extraction target directory.");
        }
    }

}
