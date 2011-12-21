package npanday.executable.execution;

import com.google.common.base.Preconditions;
import org.codehaus.plexus.logging.Logger;
import org.codehaus.plexus.util.cli.StreamConsumer;

/**
 * Provides behavior for determining whether the command utility wrote anything to the Standard Error Stream.
 * NOTE: I am using this to decide whether to fail the NPanday build. If the compiler implementation chooses
 * to write warnings to the error stream, then the build will fail on warnings!!!
 *
 * @author Shane Isbell
 */
class ErrorStreamConsumer
    implements StreamConsumer
{

    /**
     * Is true if there was anything consumed from the stream, otherwise false
     */
    private boolean error;

    /**
     * Buffer to store the stream
     */
    private StringBuffer sbe = new StringBuffer();

    private Logger logger;

    public ErrorStreamConsumer( Logger logger )
    {
        Preconditions.checkArgument( logger != null, "logger must not be null" );
        this.logger = logger;

        error = false;
    }

    public void consumeLine( String line )
    {
        sbe.append( line );
        if ( logger != null )
        {
            logger.info( line );
        }
        error = true;
    }

    /**
     * Returns false if the command utility wrote to the Standard Error Stream, otherwise returns true.
     *
     * @return false if the command utility wrote to the Standard Error Stream, otherwise returns true.
     */
    public boolean hasError()
    {
        return error;
    }

    /**
     * Returns the error stream
     *
     * @return error stream
     */
    public String toString()
    {
        return sbe.toString();
    }
}